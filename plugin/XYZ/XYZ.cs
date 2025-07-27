// Name: XYZ
// Submenu: Color
// Author: HPW-Dev
// Title: XYZ
// Version: 1.0.0
// Desc:
// Keywords:
// URL: https://github.com/HPW-dev/PDN-Plugins-by-HPW-Dev
// Help: hpwdev0@gmail.com

// For help writing a Bitmap plugin: https://boltbait.com/pdn/CodeLab/help/tutorial/bitmap/

#region UICode
DoubleSliderControl x_val = 0; // [-2,2] X
DoubleSliderControl y_val = 0; // [-2,2] Y
DoubleSliderControl z_val = 0; // [-2,2] Z
#endregion

unsafe static double clamp(double val, double min, double max) {
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

unsafe static double lab_f(double t) {
    return (t > 0.008856)
      ? (Math.Pow(t, 1.0 / 3.0))
      : ((7.787 * t) + (16.0 / 116.0));
}

unsafe static double lab_f_inv(double ft) {
    double t = Math.Pow(ft, 3.0);
    return (t > 0.008856)
      ? t
      : ((ft - 16.0 / 116.0) / 7.787);
}

class sRGB {
  public double r, g, b;

  public sRGB(double r_ = 0, double g_ = 0, double b_ = 0) {
    r = r_;
    g = g_;
    b = b_;
  }

  public sRGB(ColorBgra src) {
    r = linear_to_srgb(src.R / 255.0);
    g = linear_to_srgb(src.G / 255.0);
    b = linear_to_srgb(src.B / 255.0);
  }

  public ColorBgra bgra() {
    var ret = new ColorBgra();
    ret.B = (byte)clamp(srgb_to_linear(b) * 255.0, 0, 255.0);
    ret.G = (byte)clamp(srgb_to_linear(g) * 255.0, 0, 255.0);
    ret.R = (byte)clamp(srgb_to_linear(r) * 255.0, 0, 255.0);
    ret.A = 0xFF;
    return ret;
  }
}; // sRGB

class XYZ {
  public double x, y, z;

  public XYZ() {
      x = 0;
      y = 0;
      z = 0;
  }

  public XYZ(sRGB src) {
    // Матричное преобразование
    x = 0.4124564 * src.r + 0.3575761 * src.g + 0.1804375 * src.b;
    y = 0.2126729 * src.r + 0.7151522 * src.g + 0.0721750 * src.b;
    z = 0.0193339 * src.r + 0.1191920 * src.g + 0.9503041 * src.b;
  }

  public sRGB srgb() {
    var ret = new sRGB();
    // XYZ → линейный RGB (D65)
    ret.r =  3.2404542f * x - 1.5371385f * y - 0.4985314f * z;
    ret.g = -0.9692660f * x + 1.8760108f * y + 0.0415560f * z;
    ret.b =  0.0556434f * x - 0.2040259f * y + 1.0572252f * z;
    return ret;
  }
}; // XYZ

unsafe ColorBgra32 process_color(ColorBgra32 src) {
  var rgb = new RGBd(src);
  var xyz = new XYZ(rgb);
  xyz.x += x_val;
  xyz.y += y_val;
  xyz.z += z_val;
  var ret = xyz.rgbd().bgra();
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
