// Name: Highp BRW Dither
// Submenu: Color
// Author: HPW-Dev
// Title: Highp BRW Dither
// Version: 1.0.0
// Desc: Высокоточный дизеринг ЧБ и ЧКБ
// Keywords: Desaturation|Decolorization|1bit|monochrome|BW|BWR|Dither|Dithering
// URL: hpwdev0@gmail.com
// Help:
#region UICode
CheckboxControl g_use_red_channel = true; // красный цвет
ListBoxControl g_gamma_correction_t = 1; // гамма коррекция|none|linear -> sRGB|sRGB -> linear|input sRGB|input linear|output sRGB|output linear
//ListBoxControl g_dithering_t = 3; // дизеринг|none|threshold|simple error|Atkinson|JJN|Bayer 16x16|H-Line|noise|blue noise
ListBoxControl g_dithering_t = 2; // дизеринг|none|threshold|Atkinson|JJN|noise
ListBoxControl g_Desaturation_t = 0; // режим обесцвечивания|bt.709|0.35 0.5 0.15|bt.601|bt.2001|average|min|MinMax|max|red only|green only|blue only|Euclide
DoubleSliderControl g_brightness = 0.0; // [-1,1] яркость
DoubleSliderControl g_contrast = 1.0; // [0,3] контраст
DoubleSliderControl g_gamma = 1.0; // [0,3] гамма
DoubleSliderControl g_threshold = 0.5; // [0,1] порог белого
DoubleSliderControl g_dither_offset = 0.0; // [-0.5,0.5] смещение
DoubleSliderControl g_dither_amplify = 1.0; // [0,2] усиление
#endregion

// режимы гамма коррекции
enum Gamma_correction_t {
  none = 0,
  linear_to_srgb,
  srgb_to_linear,
  in_srgb,
  in_linear,
  out_srgb,
  out_linear,
}

// для выбора алгоритма дизеринга
enum Dithering_t {
  none = 0,        // без дизера
  threshold,       // грубый порог
  //simple_error,    // одномерное распределение ошибки
  atkinson,
  jjn,             // Jarvis Judice Ninke
  //bayer_16x16,
  //hline,           // горизонтальные полосы
  noise,           // случайный шум
  //blue_noise,
}

// режимы вычисления яркости
enum Desaturation_t {
  bt709 = 0,
  R035_G05_B015,
  bt601,
  bt2001,
  average,
  min,
  minmax,
  max,
  only_red,
  only_green,
  only_blue,
  euclide,
}

// какие дизеринги можно юзать в многопотоке
private static Dictionary<Dithering_t, bool> MULTITHREAD_CHECK = new Dictionary<Dithering_t, bool> {
  {Dithering_t.none, true},
  {Dithering_t.threshold, true},
  //{Dithering_t.simple_error, false},
  {Dithering_t.atkinson, false},
  {Dithering_t.jjn, false},
  //{Dithering_t.bayer_16x16, true},
  //{Dithering_t.hline, true},
  {Dithering_t.noise, true},
  //{Dithering_t.blue_noise, true},
};

// цвет: чб, чёрный-красный, красный-белый
public enum Local_color_mode {bw = 0, black_red, red_white}

// цвет для внутренних преобразований
public struct Local_color {
  public double value;
  public Local_color_mode mode;
}

unsafe double clamp(double val, double min, double max) {
  if (val < min)
    val = min;
  if (val > max)
    val = max;
  return val;
}

// double to byte
unsafe byte d2b(double val) { return (byte)clamp(val * 255.0, 0.0, 255.0); }

// byte to double
unsafe double b2d(byte val) { return (byte)val / 255.0; }

unsafe double gamma(double src, double gamma_value) {
  return Math.Pow(src, 1.0 / gamma_value);
}

// из sRGB компонента в линейный
unsafe double to_linear_space(double srgb_color) {
  if (srgb_color <= 0.04045)
    return srgb_color / 12.02;
  return Math.Pow(((srgb_color + 0.055) / 1.055), 2.4);
}

// из линейного компонента в sRGB
unsafe double to_srgb_space(double linear_color) {
  if (linear_color <= 0.0031308)
    return 12.92 * linear_color;
  return (1.055 * Math.Pow(linear_color, 1.0 / 2.4)) - 0.055;
}

// гамма коррекция для входных цветовых компонент
unsafe double input_gamma_process(double src) {
  switch ((Gamma_correction_t)g_gamma_correction_t) {
    default:
    case Gamma_correction_t.out_srgb:
    case Gamma_correction_t.out_linear:
    case Gamma_correction_t.none:
      break;

    case Gamma_correction_t.in_linear:
    case Gamma_correction_t.linear_to_srgb:
      src = to_linear_space(src);
      break;

    case Gamma_correction_t.in_srgb:
    case Gamma_correction_t.srgb_to_linear:
      src = to_srgb_space(src);
      break;
  }

  return src;
}

// гамма коррекция для выходного оттенка
unsafe double output_gamma_process(double src) {
  switch ((Gamma_correction_t)g_gamma_correction_t) {
    default:
    case Gamma_correction_t.none:
      break;

    case Gamma_correction_t.out_srgb:
      src = to_srgb_space(src);
      break;

    case Gamma_correction_t.out_linear:
      src = to_linear_space(src);
      break;

    case Gamma_correction_t.in_linear:
    case Gamma_correction_t.in_srgb:
      break;

    case Gamma_correction_t.linear_to_srgb:
      src = to_srgb_space(src);
      break;

    case Gamma_correction_t.srgb_to_linear:
      src = to_linear_space(src);
      break;
  }

  return src;
}

// конвертирует в серые оттенки
unsafe double desaturate(double r, double g, double b) {
  double ret;

  switch ((Desaturation_t)g_Desaturation_t) {
    case Desaturation_t.bt2001: { ret = r * 0.2627 + g * 0.6780 + b * 0.0593; break; }
    default:
    case Desaturation_t.bt709: { ret = r * 0.2126 + g * 0.7152 + b * 0.0722; break; }
    case Desaturation_t.bt601: { ret = r * 0.299 + g * 0.587 + b * 0.114; break; }
    case Desaturation_t.R035_G05_B015: { ret = r * 0.35 + g * 0.5 + b * 0.15; break; }
    case Desaturation_t.euclide: { ret = Math.Sqrt(Math.Pow(r, 2.0) + Math.Pow(g, 2.0) + Math.Pow(b, 2.0)); break; }
    case Desaturation_t.min: {
      ret = Math.Max(r, g);
      ret = Math.Max(ret, b);
      break;
    }
    case Desaturation_t.max: {
      ret = Math.Min(r, g);
      ret = Math.Min(ret, b);
      break;
    }
    case Desaturation_t.minmax: {
      var A = Math.Max(r, g);
      A = Math.Max(A, b);
      var B = Math.Min(r, g);
      B = Math.Min(B, b);
      ret = (A + B) * 0.5;
      break;
    }
    case Desaturation_t.average: { ret = (r + g + b) / 3.0; break; }
    case Desaturation_t.only_red: { ret = r; break; }
    case Desaturation_t.only_green: { ret = g; break; }
    case Desaturation_t.only_blue: { ret = b; break; }
  }

  return ret;
}


unsafe double brightness(double src) {
  return src + g_brightness;
}

unsafe double contrast(double src) {
  return (src - 0.5) * g_contrast + 0.5;
}

unsafe double gamma(double src) {
  return Math.Pow(src, 1.0 / g_gamma);
}


unsafe Local_color to_local_color(ColorBgra src) {
  var ret = new Local_color();

  double b = b2d(src.B);
  double g = b2d(src.G);
  double r = b2d(src.R);

  // todo check red color

  // gamma processing:
  r = input_gamma_process(r);
  g = input_gamma_process(g);
  b = input_gamma_process(b);

  ret.value = desaturate(r, g, b);
  ret.value = brightness(ret.value);
  ret.value = contrast(ret.value);
  ret.value = gamma(ret.value);
  return ret;
}

unsafe ColorBgra to_bgra(Local_color src) {
  double luma = src.value;
  var ret = new ColorBgra();
  
  // определить оттенки 
  if (src.mode == Local_color_mode.bw) {
    ret.B = d2b(luma);
    ret.G = d2b(luma);
    ret.R = d2b(luma);
  } else if (src.mode == Local_color_mode.black_red) {
    ret.B = 0;
    ret.G = 0;
    ret.R = d2b(luma);
  } else {
    // TODO red-white
    ret.B = d2b(luma);
    ret.G = d2b(luma);
    ret.R = d2b(luma);
  }
  
  ret.A = 255;
  return ret;
}

// получить пиксель с картинки без проверки выхода индексов за границы
unsafe ColorBgra pixel_unsafe(ColorBgra* src, int x, int y, int w) {
  return src[y * w + x];
}

unsafe void set_pixel_unsafe(Local_color color, ColorBgra* dst, int x, int y, int w) {
  dst[y * w + x] = to_bgra(color);
}

unsafe double threshold(double src) {
  return src >= g_threshold ? 1.0 : 0.0;;
}

unsafe double noise_dither(double src) {
  Random generator = new Random();
  double add = (generator.NextDouble() - 0.5 - g_dither_offset) * g_dither_amplify;
  return threshold(output_gamma_process(src + add));
}

// обрабатывает цвета в многопотоке
unsafe ColorBgra multithread_processing(ColorBgra src, int x, int y) {
  var local_color = to_local_color(src);

  switch ((Dithering_t)g_dithering_t) {
    default:
    case Dithering_t.none: local_color.value = output_gamma_process(local_color.value); break;
    case Dithering_t.threshold: local_color.value = threshold(output_gamma_process(local_color.value)); break;
    //case Dithering_t.bayer_16x16: /*TODO*/ break;
    //case Dithering_t.hline: /*TODO*/ break;
    case Dithering_t.noise: local_color.value = noise_dither(local_color.value); break;
    //case Dithering_t.blue_noise: /*TODO*/ break;
  }

  return to_bgra(local_color);
}

unsafe void dither_atkinson(ColorBgra* dst, ColorBgra* src, int w, int h) {
  int sz = w * h;
  Local_color[] buffer = new Local_color[sz];
  for (int i = 0; i < sz; i++)
    buffer[i] = to_local_color(src[i]);

  for (int y = 0; y < h-2; y++) {
    if (IsCancelRequested) return;

    for (int x = 1; x < w-2; x++) {
      var old_pixel = buffer[y * w + x];
      var new_pixel = old_pixel;
      new_pixel.value = threshold(output_gamma_process(old_pixel.value));
      buffer[y * w + x] = new_pixel;
      double q_error = old_pixel.value - new_pixel.value;
      buffer[(y+0) * w + (x+2)].value += q_error * 0.125 * g_dither_amplify + g_dither_offset;
      buffer[(y+0) * w + (x+1)].value += q_error * 0.125 * g_dither_amplify + g_dither_offset;
      buffer[(y+1) * w + (x-1)].value += q_error * 0.125 * g_dither_amplify + g_dither_offset;
      buffer[(y+1) * w + (x+0)].value += q_error * 0.125 * g_dither_amplify + g_dither_offset;
      buffer[(y+1) * w + (x+1)].value += q_error * 0.125 * g_dither_amplify + g_dither_offset;
      buffer[(y+2) * w + (x+0)].value += q_error * 0.125 * g_dither_amplify + g_dither_offset;
    }
  }

  for (int i = 0; i < sz; i++)
    dst[i] = to_bgra(buffer[i]);
}

unsafe void dither_jjn(ColorBgra* dst, ColorBgra* src, int w, int h) {
  int sz = w * h;
  Local_color[] buffer = new Local_color[sz];
  for (int i = 0; i < sz; i++)
    buffer[i] = to_local_color(src[i]);

  for (int y = 0; y < h-2; y++) {
    if (IsCancelRequested) return;

    for (int x = 1; x < w-2; x++) {
      var old_pixel = buffer[y * w + x];
      var new_pixel = old_pixel;
      new_pixel.value = threshold(output_gamma_process(old_pixel.value));
      buffer[y * w + x] = new_pixel;
      double q_error = old_pixel.value - new_pixel.value;
      buffer[(y+0) * w + (x+1)].value += q_error * (7.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+0) * w + (x+2)].value += q_error * (5.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+1) * w + (x-2)].value += q_error * (3.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+1) * w + (x-1)].value += q_error * (5.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+1) * w + (x+0)].value += q_error * (7.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+1) * w + (x+1)].value += q_error * (5.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+1) * w + (x+2)].value += q_error * (3.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+2) * w + (x-2)].value += q_error * (1.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+2) * w + (x-1)].value += q_error * (3.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+2) * w + (x+0)].value += q_error * (5.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+2) * w + (x+1)].value += q_error * (3.0/48.0) * g_dither_amplify + g_dither_offset;
      buffer[(y+2) * w + (x+2)].value += q_error * (1.0/48.0) * g_dither_amplify + g_dither_offset;
    }
  }

  for (int i = 0; i < sz; i++)
    dst[i] = to_bgra(buffer[i]);
}

// обрабатывает цвета последовательно
unsafe void single_core_processing(ColorBgra* dst, ColorBgra* src, int w, int h) {
  switch ((Dithering_t)g_dithering_t) {
    default:
    case Dithering_t.atkinson: dither_atkinson(dst, src, w, h); break;
    case Dithering_t.jjn: dither_jjn(dst, src, w, h); break;
  }
}

protected override void OnDispose(bool disposing) {
  if (disposing) {
    // Release any surfaces or effects you've created
  }
  base.OnDispose(disposing);
}

unsafe void PreRender(Surface dst, Surface src) {
  // проверить что эффект может работать в одном потоке
  if (MULTITHREAD_CHECK[(Dithering_t)g_dithering_t])
    return;

  ColorBgra* src_p = src.GetPointPointerUnchecked(0, 0);
  ColorBgra* dst_p = dst.GetPointPointerUnchecked(0, 0);
  single_core_processing(dst_p, src_p, src.Width, src.Height);
}

unsafe void Render(Surface dst, Surface src, Rectangle rect) {
  // проверить что эффект может работать в многопотоке
  if (!MULTITHREAD_CHECK[(Dithering_t)g_dithering_t])
    return;

  for (int y = rect.Top; y < rect.Bottom; y++) {
    if (IsCancelRequested) return;

    ColorBgra* src_p = src.GetPointPointerUnchecked(rect.Left, y);
    ColorBgra* dst_p = dst.GetPointPointerUnchecked(rect.Left, y);

    for (int x = rect.Left; x < rect.Right; x++) {
      *dst_p = multithread_processing(*src_p, x, y);
      src_p++;
      dst_p++;
    }
  }
}
