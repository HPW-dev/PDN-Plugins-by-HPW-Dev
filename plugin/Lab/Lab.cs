// Name: Lab
// Submenu: Color
// Author: HPW-Dev
// Title: Lab
// Version: 1.0.0
// Desc:
// Keywords:
// URL: https://github.com/HPW-dev/PDN-Plugins-by-HPW-Dev
// Help: hpwdev0@gmail.com

// For help writing a Bitmap plugin: https://boltbait.com/pdn/CodeLab/help/tutorial/bitmap/

#region UICode
DoubleSliderControl l_val = 0; // [-100,100] L
DoubleSliderControl a_val = 0; // [-180,180] a
DoubleSliderControl b_val = 0; // [-180,180] b
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

class Lab {
  public double l, a, b;

  public Lab(XYZ src) {
    // Нормализация XYZ к белой точке D65
    double x_n = src.x / 0.95047;
    double y_n = src.y / 1.00000;
    double z_n = src.z / 1.08883;

    // XYZ → Lab
    double fx = lab_f(x_n);
    double fy = lab_f(y_n);
    double fz = lab_f(z_n);

    l = 116.0 * fy - 16.0;
    a = 500.0 * (fx - fy);
    b = 200.0 * (fy - fz);
  }

  public XYZ xyz() {
    var ret = new XYZ();
    // Lab → XYZ
    double fy = (l + 16.0) / 116.0;
    double fx = fy + (a / 500.0);
    double fz = fy - (b / 200.0);

    ret.x = 0.95047f * lab_f_inv(fx);
    ret.y = 1.00000f * lab_f_inv(fy);
    ret.z = 1.08883f * lab_f_inv(fz);
    return ret;
  }
}; // Lab

unsafe ColorBgra32 process_color(ColorBgra32 src) {
  var rgb = new sRGB(src);
  var xyz = new XYZ(rgb);
  var lab = new Lab(xyz);
  lab.l += l_val;
  lab.a += a_val;
  lab.b += b_val;
  var ret = lab.xyz().srgb().bgra();
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
