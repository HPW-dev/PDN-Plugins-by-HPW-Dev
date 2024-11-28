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
#endregion

enum Dummy {
  TEST_A = 0,
  TEST_B,
}

private static Dictionary<Dummy, bool> test_dict = new Dictionary<Dummy, bool> {
  {Dummy.TEST_A, true},
  {Dummy.TEST_B, false},
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

// обрабатывает цвета в многопотоке
unsafe ColorBgra multithread_processing(ColorBgra src) {
  return src;
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
  // TODO проверять какой алгоритм сингл тредный
}

protected override void OnDispose(bool disposing) {
  if (disposing) {
    // Release any surfaces or effects you've created
  }
  base.OnDispose(disposing);
}

unsafe void PreRender(Surface dst, Surface src) {
  ColorBgra* src_p = src.GetPointPointerUnchecked(0, 0);
  ColorBgra* dst_p = dst.GetPointPointerUnchecked(0, 0);
  single_core_processing(dst_p, src_p, src.Width, src.Height);
}

unsafe void Render(Surface dst, Surface src, Rectangle rect) { 
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
