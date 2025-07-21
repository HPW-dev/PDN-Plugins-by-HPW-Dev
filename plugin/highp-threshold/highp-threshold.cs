// Name: High Precission Black/White Thresholding
// Submenu: Color
// Author: HPW-Dev
// Title: High Precission Black/White Thresholding
// Version: 1.0.0
// Desc: ЧБ порог высокой точности с гамма-коррекцией
// Keywords: highp|HighPrecission|Thresholding|Threshold|desaturation|BW|blackwhite
// URL: https://github.com/HPW-dev/PDN-Plugins-by-HPW-Dev
// Help: hpwdev0@gmail.com
// Force Aliased Selection
#region UICode
ListBoxControl g_Desaturation_t = 0; // режим обесцвечивания|bt.709|0.35 0.5 0.15|bt.601|bt.2001|average|min|MinMax|max|red only|green only|blue only|Euclide
CheckboxControl pre_inversion_val = false; // Pre-inversion
DoubleSliderControl gamma_val = 1; // [0.001,2.4] Gamma
DoubleSliderControl brightness_val = 0; // [-1,1] Brightness
DoubleSliderControl contrast_val = 1; // [0,3] Contrast
CheckboxControl post_inversion_val = false; // Post-inversion
#endregion

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

// конвертирует в серые оттенки
unsafe double desaturate(ColorBgra src) {
  double b = src.B / 255.0;
  double g = src.G / 255.0;
  double r = src.R / 255.0;
  if (pre_inversion_val) {
    b = 1.0 - b;
    g = 1.0 - g;
    r = 1.0 - r;
  }
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

unsafe double clamp(double val, double min, double max) {
  if (val < min)
    val = min;
  if (val > max)
    val = max;
  return val;
}

// сервый в RGB пейнта
unsafe ColorBgra gray_to_rgb(double luma, byte alpha) {
  var ret = new ColorBgra();
  if (post_inversion_val)
    luma = 1.0 - luma;
  ret.B = (byte)(clamp(luma * 255.0, 0.0, 255.0));
  ret.G = (byte)(clamp(luma * 255.0, 0.0, 255.0));
  ret.R = (byte)(clamp(luma * 255.0, 0.0, 255.0));
  ret.A = alpha;
  return ret;
}

// гамма
unsafe double gamma(double luma, double val) {
  return Math.Pow(luma, 1.0 / val);
}

// контраст
unsafe double contrast(double luma, double val) {
  return ((luma - 0.5) * val) + 0.5;
}

// яркость
unsafe double brightness(double luma, double val) {
  return luma + val;
}

unsafe void PreRender(Surface dst, Surface src) {}

unsafe void Render(Surface dst, Surface src, Rectangle rect) {
  for (int y = rect.Top; y < rect.Bottom; y++) {
    if (IsCancelRequested) return;
    ColorBgra* srcPtr = src.GetPointPointerUnchecked(rect.Left, y);
    ColorBgra* dstPtr = dst.GetPointPointerUnchecked(rect.Left, y);
    
    for (int x = rect.Left; x < rect.Right; x++) {
      double luma = desaturate(*srcPtr);
      luma = gamma(luma, gamma_val);
      luma = brightness(luma, brightness_val);
      luma = contrast(luma, contrast_val);
      *dstPtr = gray_to_rgb(luma, (*srcPtr).A);
      srcPtr++;
      dstPtr++;
    }
  }
}

