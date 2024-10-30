// Name: BWR Threshold RGB
// Submenu: Color
// Author: HPW-Dev
// Title: BW color correction with red-channel support
// Version: 2.1
// Desc: for comix
// Keywords: BW|RGB|red|color|desaturation|threshold
// URL: hpwdev0@gmail.com
// Help: 2024. Free

// Force Aliased Selection
#region UICode
ListBoxControl g_color_processing_mode = 0; // color processing|none|sRGB|Linear
DoubleSliderControl g_pre_brightness_r = 0; // [-1,1] pre brightness red
DoubleSliderControl g_pre_brightness_g = 0; // [-1,1] pre brightness green
DoubleSliderControl g_pre_brightness_b = 0; // [-1,1] pre brightness blue
DoubleSliderControl g_pre_gamma_r = 1; // [1E-07,2] pre gamma red
DoubleSliderControl g_pre_gamma_g = 1; // [1E-07,2] pre gamma green
DoubleSliderControl g_pre_gamma_b = 1; // [1E-07,2] pre gamma blue
DoubleSliderControl g_pre_contrast_r = 1; // [1E-07,2] pre contrast red
DoubleSliderControl g_pre_contrast_g = 1; // [1E-07,2] pre contrast green
DoubleSliderControl g_pre_contrast_b = 1; // [1E-07,2] pre contrast blue
ListBoxControl g_destaturate_mode = 0; // desaturation mode|BT2001|BT709|BT601|0.35-0.5-0.15|Euclidean distance|MIN|MAX|MINMAX|average|only red|only green|only blue
DoubleSliderControl g_brightness = 0; // [-1,1] brightness
DoubleSliderControl g_gamma = 1; // [1E-07,2] gamma
DoubleSliderControl g_contrast = 1; // [1E-07,4] contrast
CheckboxControl g_use_thresholding = true; // use thresholding
CheckboxControl g_use_red_channel = true; // use red-channel
DoubleSliderControl g_threashold = 0.5; // [0,1] threshold
DoubleSliderControl g_threashold_red = 0.830; // [0,1] threshold red
#endregion

// режимы гамма-коррекции
enum Color_processing_mode {
  NONE = 0,
  SRGB,
  LINEAR,
}

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
  AVERAGE,
  ONLY_RED,
  ONLY_GREEN,
  ONLY_BLUE,
}

unsafe double clamp(double val, double min, double max) {
  if (val < min)
    val = min;
  if (val > max)
    val = max;
  return val;
}

unsafe byte d2b(double val) { return (byte)clamp(val * 255.0, 0.0, 255.0); }
unsafe double b2d(byte val) { return (byte)val / 255.0; }

unsafe double desaturate(double r, double g, double b) {
  double ret;
  switch ((Desaturate_mode)g_destaturate_mode) {
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
      ret = (A + B) / 2.0;
      break;
    }
    case Desaturate_mode.AVERAGE: { ret = (r + g + b) / 3.0; break; }
    case Desaturate_mode.ONLY_RED: { ret = r; break; }
    case Desaturate_mode.ONLY_GREEN: { ret = g; break; }
    case Desaturate_mode.ONLY_BLUE: { ret = b; break; }
  }
  return ret;
}

unsafe double threshold(double src) {
  if (src > g_threashold)
    return 1;
  return 0;
}

unsafe double brightness(double src, double val) {
  return src + val;
}

unsafe double gamma(double src, double val) {
  return src * val;
}

unsafe double contrast(double src, double val) {
  return (src - 0.5) * val + 0.5;
}

unsafe bool check_if_red(double r, double g, double b, double threshold) {
  return r - ((g + b) * 0.5) >= (1.0 - threshold);
}

unsafe ColorBgra process_color(ColorBgra src) {
  ColorBgra ret = src;
  double r = b2d(src.R);
  double g = b2d(src.G);
  double b = b2d(src.B);
  r = brightness(r, g_pre_brightness_r);
  g = brightness(g, g_pre_brightness_g);
  b = brightness(b, g_pre_brightness_b);
  r = gamma(r, g_pre_gamma_r);
  g = gamma(g, g_pre_gamma_g);
  b = gamma(b, g_pre_gamma_b);
  r = contrast(r, g_pre_contrast_r);
  g = contrast(g, g_pre_contrast_g);
  b = contrast(b, g_pre_contrast_b);

  var is_red = check_if_red(r, g, b, g_threashold_red);
  var l = desaturate(r, g, b);

  l = brightness(l, g_brightness);
  l = gamma(l, g_gamma);
  l = contrast(l, g_contrast);

  
  if (g_use_thresholding) {
    l = threshold(l);
    ret.R = d2b(l);
    ret.G = d2b(l);
    ret.B = d2b(l);
    if (g_use_red_channel && is_red) {
      ret.G = ret.B = 0;
      ret.R = 255;
    }
  } else {
    if (g_use_red_channel)
      ret.G = ret.B = d2b(l);
    else
      ret.R = ret.G = ret.B = d2b(l);
  }

  return ret;
}

void PreRender(Surface dst, Surface src) {}

unsafe void Render(Surface dst, Surface src, Rectangle rect) {
  for (int y = rect.Top; y < rect.Bottom; y++) {
    if (IsCancelRequested) return;

    ColorBgra* srcPtr = src.GetPointPointerUnchecked(rect.Left, y);
    ColorBgra* dstPtr = dst.GetPointPointerUnchecked(rect.Left, y);

    for (int x = rect.Left; x < rect.Right; x++) {
      ColorBgra SrcPixel = *srcPtr;
      ColorBgra DstPixel = *dstPtr;
      *dstPtr = process_color(SrcPixel);
      srcPtr++;
      dstPtr++;
    }
  }
}
