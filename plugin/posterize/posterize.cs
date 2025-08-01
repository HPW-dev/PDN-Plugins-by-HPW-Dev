// Name: Posterization
// Submenu: Color
// Author: HPW-Dev
// Title: Color posterization
// Version: 1.0
// Desc: 
// Keywords: Posterization|color
// URL: https://github.com/HPW-dev/PDN-Plugins-by-HPW-Dev
// Help: hpwdev0@gmail.com

#region UICode
CheckboxControl use_gamma_corr = false; // use gamma correction
IntSliderControl levels = 4; // [2,255] levels
#endregion

public static double clamp(double val, double min, double max) {
  if (val < min)
    val = min;
  if (val > max)
    val = max;
  return val;
}

// Гамма-коррекция (линейный → sRGB)
unsafe static double linear_to_srgb(double c) {
  return (c <= 0.0031308)
    ? (12.92 * c)
    : (1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055);
}

unsafe static double srgb_to_linear(double c) {
  return (c <= 0.04045)
    ? (c / 12.92)
    : (Math.Pow((c + 0.055) / 1.055, 2.4));
}

class sRGB {
  public double r, g, b;

  unsafe public sRGB(double r_ = 0, double g_ = 0, double b_ = 0) {
    r = r_;
    g = g_;
    b = b_;
  }

  unsafe public sRGB(ColorBgra src, bool use_gamma_corr) {
    if (use_gamma_corr) {
      r = srgb_to_linear(src.R / 255.0);
      g = srgb_to_linear(src.G / 255.0);
      b = srgb_to_linear(src.B / 255.0);
    } else {
      r = src.R / 255.0;
      g = src.G / 255.0;
      b = src.B / 255.0;
    }
  }

  unsafe public ColorBgra bgra(bool use_gamma_corr) {
    var ret = new ColorBgra();
    if (use_gamma_corr) {
      ret.B = (byte)clamp(linear_to_srgb(b) * 255.0, 0, 255.0);
      ret.G = (byte)clamp(linear_to_srgb(g) * 255.0, 0, 255.0);
      ret.R = (byte)clamp(linear_to_srgb(r) * 255.0, 0, 255.0);
    } else {
      ret.B = (byte)clamp(b * 255.0, 0, 255.0);
      ret.G = (byte)clamp(g * 255.0, 0, 255.0);
      ret.R = (byte)clamp(r * 255.0, 0, 255.0);
    }
    ret.A = 0xFF;
    return ret;
  }
}; // sRGB

unsafe double posterize(double src, int levels) {
  /*
  var step = 255 / (levels-1);
  var srci = (int)(src * 255.0);
  return (Math.Round((double)srci / step) * step) / 255.0;
  */
  var step1 = 1.0 / (levels);
  var step2 = 1.0 / (levels-1);
  var colors = src / step1;
  return Math.Round(colors-0.5) * step2;
}

unsafe ColorBgra32 process_color(ColorBgra32 src) {
  var rgb = new sRGB(src, use_gamma_corr);
  rgb.r = posterize(rgb.r, levels);
  rgb.g = posterize(rgb.g, levels);
  rgb.b = posterize(rgb.b, levels);
  var ret = rgb.bgra(false);
  ret.A = src.A;
  return ret;
}

protected override void OnRender(IBitmapEffectOutput output) {
  using IEffectInputBitmap<ColorBgra32> sourceBitmap = Environment.GetSourceBitmapBgra32();
  using IBitmapLock<ColorBgra32> sourceLock = sourceBitmap.Lock(new RectInt32(0, 0, sourceBitmap.Size));
  RegionPtr<ColorBgra32> sourceRegion = sourceLock.AsRegionPtr();

  RectInt32 outputBounds = output.Bounds;
  using IBitmapLock<ColorBgra32> outputLock = output.LockBgra32();
  RegionPtr<ColorBgra32> outputSubRegion = outputLock.AsRegionPtr();
  var outputRegion = outputSubRegion.OffsetView(-outputBounds.Location);

  for (int y = outputBounds.Top; y < outputBounds.Bottom; ++y) {
    if (IsCancelRequested) return;

    for (int x = outputBounds.Left; x < outputBounds.Right; ++x)
      outputRegion[x,y] = process_color(sourceRegion[x,y]);
  }
}
