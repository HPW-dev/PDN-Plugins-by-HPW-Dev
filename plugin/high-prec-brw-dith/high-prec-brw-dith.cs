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
CheckboxControl g_use_gamma_correction = true; // гамма коррекция
ListBoxControl g_dithering_t = 3; // дизеринг|none|threshold|simple error|Atkinson|JJN|Bayer 16x16|H-Line|noise|blue noise
ListBoxControl g_destaturation_t = 0; // режим обесцвечивания|bt.709|0.35 0.5 0.15|bt.601|bt.2001|average|min|MinMax|max|red only|green only|blue only|Euclide
#endregion

// для выбора алгоритма дизеринга
enum Dithering_t {
  none = 0,        // без дизера
  threshold,       // грубый порог
  simple_error,    // одномерное распределение ошибки
  atkinson,
  jjn,             // Jarvis Judice Ninke
  bayer_16x16,
  hline,           // горизонтальные полосы
  noise,           // случайный шум
  blue_noise,
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
  {Dithering_t.simple_error, false},
  {Dithering_t.atkinson, false},
  {Dithering_t.jjn, false},
  {Dithering_t.bayer_16x16, true},
  {Dithering_t.hline, true},
  {Dithering_t.noise, true},
  {Dithering_t.blue_noise, true},
};

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

// получить пиксель с картинки без проверки выхода индексов за границы
unsafe ColorBgra pixel_unsafe(ColorBgra* src, int x, int y, int w) {
  return src[y * w + x];
}

// обрабатывает цвета в многопотоке
unsafe ColorBgra multithread_processing(ColorBgra src) {
  //return src; // TODO
  var color = new ColorBgra();
  color.A = 255;
  color.R = 255;
  return color;
}

// обрабатывает цвета последовательно
unsafe void single_core_processing(ColorBgra* dst, ColorBgra* src, int w, int h) {
  /*
  for (int i = 0; i < sz; i++) {
    //*dst_p = single_core_processing(*src_p);
    *dst = *src;
    src++;
    dst++;
  }
  */

  for (int i = 0; i < w * h; i++) {
    //*dst_p = single_core_processing(*src_p);
    var color = new ColorBgra();
    color.A = 255;
    color.B = 255;
    *dst = color;
    src++;
    dst++;
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
      *dst_p = multithread_processing(*src_p);
      src_p++;
      dst_p++;
    }
  }
}
