// Name: Desaturator+
// Submenu: Color
// Author: HPW-Dev
// Title: Desaturator+
// Version: 1.1.0
// Desc: Обесцвечивание картинки
// Keywords: Desaturation|Decolorization|grayscale
// URL: hpwdev0@gmail.com
// Help:
#region UICode
DoubleSliderControl g_gamma_r = 1; // [0,2.6] Gamma Red
DoubleSliderControl g_gamma_g = 1; // [0,2.6] Gamma Green
DoubleSliderControl g_gamma_b = 1; // [0,2.6] Gamma Blue
ListBoxControl g_destaturation_mode = 0; // desaturation mode|BT2001|BT709|BT601|0.35-0.5-0.15|Euclidean distance|Min|Max|MinMax|average|only red|only green|only blue
ListBoxControl g_color_processing_mode = 0; // input processing|none|sRGB|linear RGB
CheckboxControl g_use_color_processing_output = true; // use output processing
DoubleSliderControl g_gamma_out = 1; // [0,2.6] Gamma post process
#endregion

// режимы обесцвечивания
enum Desaturate_mode {
  BT2001 = 0,
  BT709,
  BT601,
  UNK_035_05_015,
  EUCLIDE,
  MIN,
  MAX,
  MINMAX,
  AVR,
  RED,
  GREEN,
  BLUE,
}

enum Processing_mode {
  NONE = 0,
  SRGB,
  LINEAR,
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

unsafe double desaturate(double r, double g, double b) {
  double ret;

  switch ((Desaturate_mode)g_destaturation_mode) {
    default:
    case Desaturate_mode.BT2001: { ret = r * 0.2627 + g * 0.6780 + b * 0.0593; break; }
    case Desaturate_mode.BT709: { ret = r * 0.2126 + g * 0.7152 + b * 0.0722; break; }
    case Desaturate_mode.BT601: { ret = r * 0.299 + g * 0.587 + b * 0.114; break; }
    case Desaturate_mode.UNK_035_05_015: { ret = r * 0.35 + g * 0.5 + b * 0.15; break; }
    case Desaturate_mode.EUCLIDE: { ret = Math.Sqrt(Math.Pow(r, 2.0) + Math.Pow(g, 2.0) + Math.Pow(b, 2.0)); break; }
    case Desaturate_mode.MIN: {
      ret = Math.Max(r, g);
      ret = Math.Max(ret, b);
      break;
    }
    case Desaturate_mode.MAX: {
      ret = Math.Min(r, g);
      ret = Math.Min(ret, b);
      break;
    }
    case Desaturate_mode.MINMAX: {
      var A = Math.Max(r, g);
      A = Math.Max(A, b);
      var B = Math.Min(r, g);
      B = Math.Min(B, b);
      ret = (A + B) * 0.5;
      break;
    }
    case Desaturate_mode.AVR: { ret = (r + g + b) / 3.0; break; }
    case Desaturate_mode.RED: { ret = r; break; }
    case Desaturate_mode.GREEN: { ret = g; break; }
    case Desaturate_mode.BLUE: { ret = b; break; }
  }

  return ret;
}

unsafe double to_linear_space(double srgb_color) {
  if (srgb_color <= 0.04045)
    return srgb_color / 12.02;
  return Math.Pow(((srgb_color + 0.055) / 1.055), 2.4);
}

unsafe double to_srgb_space(double linear_color) {
  if (linear_color <= 0.0031308)
    return 12.92 * linear_color;
  return (1.055 * Math.Pow(linear_color, 1.0 / 2.4)) - 0.055;
}

unsafe double color_processing(double color, Processing_mode mode) {
  switch ((Processing_mode)mode) {
    default:
    case Processing_mode.NONE: break;
    case Processing_mode.LINEAR: color = to_linear_space(color); break;
    case Processing_mode.SRGB: color = to_srgb_space(color); break;
  }
  return color;
}

unsafe ColorBgra desaturate_plus(ColorBgra color) {
  var r = b2d(color.R);
  var g = b2d(color.G);
  var b = b2d(color.B);
  var input_processing_mode = (Processing_mode)g_color_processing_mode;
  r = color_processing(r, input_processing_mode);
  g = color_processing(g, input_processing_mode);
  b = color_processing(b, input_processing_mode);
  r = gamma(r, g_gamma_r);
  g = gamma(g, g_gamma_g);
  b = gamma(b, g_gamma_b);
  var l = desaturate(r, g, b);
  var output_processing_mode = Processing_mode.NONE;
  if (input_processing_mode != Processing_mode.NONE) {
    if (input_processing_mode == Processing_mode.LINEAR)
      output_processing_mode = Processing_mode.SRGB;
    else
      output_processing_mode = Processing_mode.LINEAR;
  }
  l = gamma(l, g_gamma_out);
  if (g_use_color_processing_output)
    l = color_processing(l, output_processing_mode);
  color.R = d2b(l);
  color.G = d2b(l);
  color.B = d2b(l);
  return color;
}

protected override void OnDispose(bool disposing) {
  if (disposing) {
    // Release any surfaces or effects you've created
  }
  base.OnDispose(disposing);
}

void PreRender(Surface dst, Surface src) {
  // ...
}

unsafe void Render(Surface dst, Surface src, Rectangle rect) { 
  for (int y = rect.Top; y < rect.Bottom; y++) {
    if (IsCancelRequested) return;
    ColorBgra* srcPtr = src.GetPointPointerUnchecked(rect.Left, y);
    ColorBgra* dstPtr = dst.GetPointPointerUnchecked(rect.Left, y);
    for (int x = rect.Left; x < rect.Right; x++) {
      ColorBgra SrcPixel = *srcPtr;
      ColorBgra DstPixel = *dstPtr;
      *dstPtr = desaturate_plus(SrcPixel);
      srcPtr++;
      dstPtr++;
    }
  }
}
