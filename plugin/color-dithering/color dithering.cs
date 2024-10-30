// Name: Color Dithering
// Submenu: Color
// Author: HPW-dev
// Title: color quantize and dither
// Version: 2.0
// Desc: AdvDith + Quant
// Keywords: contrast|quantization|desaturation|dithering|monochrome|color
// URL: hpwdev0@gmail.com
// Help: 2021-2024. Free
// Force Single Threaded

// EN interface:
#region UICode
ListBoxControl m_find = 3; // color search mode|BT2001|BT601|Euclidean distance|difference|average|only red|only green|only blue
ListBoxControl m_palette = 1; // {!m_use_file} palette|BW|3 bit|ZX Spectrum|MSX|Commodore 64|MAC 16 color|RGBI 3x level|RiscOS 16 color
CheckboxControl m_use_file = false; // use paint.net palette file
FilenameControl m_fname = @""; // {m_use_file} |txt
CheckboxControl m_use_adaptive = false; // use adaptive palette
IntSliderControl m_col_count = 16; // [2,512] {m_use_adaptive} color count
IntSliderControl m_luma = 0; // [-255,255] pre-brightness
IntSliderControl m_post_luma = 0; // [-255,255] post-brightness
IntSliderControl m_contrast = 0; // [-255,255] pre-contrast
IntSliderControl m_post_contrast = 0; // [-255,255] post-contrast
ListBoxControl m_dither = 3; // dither|disabled|ordered 2x2|ordered 3x3|ordered 4x4|ordered 8x8|ordered 16x16|line vertical|line horizontal|Floyd-Steinberg|Floyd false|Jarvis-Judice-Ninke|Stucki|Burkes|Sierra3|Sierra2|Sierra2-4A|Atkinson|random
DoubleSliderControl m_dither_mul = 1; // [0,10] dither multipler
ListBoxControl m_error = 0; // error search func|difference|difference sum|average
CheckboxControl m_use_alpha = true; // use alpha channel
#endregion

// RU interface:
/*
#region UICode
ListBoxControl m_find = 3; // режим поиска цвета в палитре|BT2001|BT601|Евклидова дистанция|разница|среднее|только красный|только зелёный|только голубой
ListBoxControl m_palette = 1; // {!m_use_file} палитра|BW|3-х битный цвет|ZX Spectrum|MSX|Commodore 64|MAC 16 color|RGBI 3x level|RiscOS 16 color
CheckboxControl m_use_file = false; // использовать paint.net файл палитры
FilenameControl m_fname = @""; // {m_use_file} |txt
CheckboxControl m_use_adaptive = false; // использовать адаптивную палитру
IntSliderControl m_col_count = 16; // [2,512] {m_use_adaptive} количество цветов
IntSliderControl m_luma = 0; // [-255,255] яркость до
IntSliderControl m_post_luma = 0; // [-255,255] яркость после
IntSliderControl m_contrast = 0; // [-255,255] контраст до
IntSliderControl m_post_contrast = 0; // [-255,255] контраст после
ListBoxControl m_dither = 3; // алгоритм дизеринга|disabled|ordered 2x2|ordered 3x3|ordered 4x4|ordered 8x8|ordered 16x16|вертикальные линии|горизонтальные линии|Floyd-Steinberg|Floyd false|Jarvis-Judice-Ninke|Stucki|Burkes|Sierra3|Sierra2|Sierra2-4A|Atkinson|случайный шум
DoubleSliderControl m_dither_mul = 1; // [0,10] параметр дизеринга
ListBoxControl m_error = 0; // алгоритм поиска ошибки дизеринга|разница|сумма разницы|среднее
CheckboxControl m_use_alpha = true; // использовать прозрачность
#endregion
*/

// алгоритмы определения разницы в цветах
enum find_e {
  BT2001 = 0,
  BT601,
  EUCLIDE,
  DIFFERENCE,
  AVERAGE,
  ONLY_RED,
  ONLY_GREEN,
  ONLY_BLUE,
}

// способы вычисления ошибки дизеринга:
enum error_e {
  diff = 0,
  diff_sum,
  avg,
}

// типы дизеринга:
enum dither_e {
  NONE_DITHER = 0,
  ORDER2X2,
  ORDER3X3,
  ORDER4X4,
  ORDER8X8,
  ORDER16X16,
  LINEV,
  LINEH,
  FLOYD,
  FLOYD_FALSE,
  JARVIS,
  STUCKI,
  BURKES,
  SIERRA3,
  SIERRA2,
  SIERRA2_4A,
  ATKINSON,
  RANDOM,
} // dither_e

// встроенные палитры
enum palette_e {
  ADAPTIVE = -1,
  BW = 0,
  _3BIT,
  MSX, ZX_SPECTRUM, COMMODORE64,
  MAC_16COL,
  RGBI_3LEVEL,
  RISCOS_16COL,
}

public static double clamp(double val, double min, double max) {
  if (val < min)
    val = min;
  if (val > max)
    val = max;
  return val;
}

struct dRGB {
  public double r, g, b;
  // int to double (0..1)
  public static double i2d(int val) { return (double)val / 255.0; }
  public static double u2d(uint val) { return (double)val / 255.0; }
  public static double b2d(byte val) { return (byte)val / 255.0; }
  public static uint d2u(double val) { return (uint)clamp(val * 255.0, 0.0, 255.0); }
  public static byte d2b(double val) { return (byte)clamp(val * 255.0, 0.0, 255.0); }
  public static dRGB make(uint bgra = 0) {
    dRGB ret = new dRGB();
    ret.b = u2d(bgra & 0xFF);
    ret.g = u2d((bgra >> 8) & 0xFF);
    ret.r = u2d((bgra >> 16) & 0xFF);
    return ret;
  }
  public ColorBgra get_bgra() {
    ColorBgra ret = new ColorBgra();
    ret.B = d2b(b);
    ret.G = d2b(g);
    ret.R = d2b(r);
    ret.A = 0xFF;
    return ret;
  }
  public dRGB(double r_ = 0, double g_ = 0, double b_ = 0) {
    r = r_;
    g = g_;
    b = b_;
  }
  public dRGB(uint bgra) { this = make(bgra); }
  public static dRGB operator + (dRGB a, double val) {
    return new dRGB(
      a.r + val,
      a.g + val,
      a.b + val
    );
  }
  public static dRGB operator - (dRGB a, double val) {
    return new dRGB(
      a.r - val,
      a.g - val,
      a.b - val
    );
  }
  public static dRGB operator * (dRGB a, double val) {
    return new dRGB(
      a.r * val,
      a.g * val,
      a.b * val
    );
  }
  public static dRGB operator / (dRGB a, double val) {
    return new dRGB(
      a.r / val,
      a.g / val,
      a.b / val
    );
  }
  public static dRGB operator + (dRGB a, dRGB b) {
    return new dRGB(
      a.r + b.r,
      a.g + b.g,
      a.b + b.b
    );
  }
  public static dRGB operator - (dRGB a, dRGB b) {
    return new dRGB(
      a.r - b.r,
      a.g - b.g,
      a.b - b.b
    );
  }
  public static dRGB operator * (dRGB a, dRGB b) {
    return new dRGB(
      a.r * b.r,
      a.g * b.g,
      a.b * b.b
    );
  }
} // dRGB

// применяет к цвету яркость и контраст
static dRGB _colcor(dRGB src, double add, double contrast) {
// яркость
  src += add;
// контраст
  src *= 255.0;
  double factor = (259.0 * (contrast + 255.0)) / (255.0 * (259.0 - contrast));
  src -= 127.0;
  src *= factor;
  src += 127.0;
  src *= 1.0 / 255.0;
  return src;
}

// контейнер растра
class Img {
  public int X = 0;
  public int Y = 0;
  public dRGB[] buffer = null;
  public Img() {}
  public Img(Surface src) {
    X = src.Width;
    Y = src.Height;
    buffer = new dRGB[X * Y];
    for (int y = 0; y < Y; y++)
    for (int x = 0; x < X; x++) {
      fast_set(x, y, dRGB.make(src[x,y].Bgra));
    }
  }
  ~Img() { buffer = null; }
  public Img make_copy() {
    Img ret = new Img();
    ret.X = X;
    ret.Y = Y;
    ret.buffer = new dRGB[X * Y];
    for (int i = 0; i < X * Y; ++i)
      ret.buffer[i] = buffer[i];
    return ret;
  }
  public void fast_read(Img src) {
    buffer = null;
    X = src.X;
    Y = src.Y;
    buffer = new dRGB[X * Y];
    for (int i = 0; i < X * Y; ++i)
      buffer[i] = src.buffer[i];
  }
  public void fast_set(int x, int y, dRGB col)
    { buffer[y * X + x] = col; }
  public dRGB fast_get(int x, int y)
    { return buffer[y * X + x]; }
  public dRGB fast_get(int i)
    { return buffer[i]; }
  // перенос буфера в peint net полотно
  public void write(Surface dst) {
    for (int y = 0; y < Y; y++)
    for (int x = 0; x < X; x++)
      dst[x,y] = fast_get(x,y).get_bgra();
  }
  public dRGB get(int x, int y) {
    if (x < 0) x = 0;
    if (x >= X) x = X - 1;
    if (y < 0) y = 0;
    if (y >= Y) y = Y - 1;
    return buffer[y * X + x];
  }
  public void set(int x, int y, dRGB col) {
    if (x < 0) x = 0;
    if (x >= X) x = X - 1;
    if (y < 0) y = 0;
    if (y >= Y) y = Y - 1;
    buffer[y * X + x] = col;
  }
  // применение настроек цветокорекции
  public void colcor(double add, double contrast) {
    for (int i = 0; i < X * Y; ++i)
      buffer[i] = _colcor(buffer[i], add, contrast);
  }
} // class Img

class Palette {
  public dRGB[] ptr = null; // палитра для квантования
  public int max = 0; // размер палитры
}

double pow2(double val) { return val * val; }

uint hex2u(char c) {
  if (c >= '0' && c <= '9')
    return (uint)(c - '0');
  if (c >= 'a' && c <= 'f')
    return (uint)(c - 'a' + 10);
  if (c >= 'A' && c <= 'F')
    return (uint)(c - 'A' + 10);
  return 0;
}

// кодирует HEX строку с кодом цвета в Paint.net цвет
dRGB str2col(string str) {
  if (str == "" || str.Length < 8)
    return new dRGB();
  uint bgra = 0;
  foreach (char ch in str) {
    bgra <<= 4;
    bgra |= hex2u(ch);
  }
  return new dRGB(bgra);
}

dRGB color_find_BT2001(dRGB src, Palette pal) {
  int best_color = 0;
  double f_min = 10.0;
  for (int i = 0; i < pal.max; ++i) {
    double fi =
      (26.0/255.0) * pow2(pal.ptr[i].b - src.b) +
      (68.0/255.0) * pow2(pal.ptr[i].g - src.g) +
      ( 6.0/255.0) * pow2(pal.ptr[i].r - src.r);
    if (fi < f_min) {
      best_color = i;
      f_min = fi;
    }
  } // for max pal
  return pal.ptr[best_color];
} // color_find_BT2001

dRGB color_find_BT601(dRGB src, Palette pal) {
  int best_color = 0;
  double f_min = 10.0;
  for (int i = 0; i < pal.max; ++i) {
    double fi =
      (30.0/255.0) * pow2(pal.ptr[i].b - src.b) +
      (59.0/255.0) * pow2(pal.ptr[i].g - src.g) +
      (11.0/255.0) * pow2(pal.ptr[i].r - src.r);
    if (fi < f_min) {
      best_color = i;
      f_min = fi;
    }
  } // for max pal
  return pal.ptr[best_color];
} // color_find_BT601

dRGB color_find_EUCLIDE(dRGB src, Palette pal) {
  int best_color = 0;
  double f_min = 10.0;
  for (int i = 0; i < pal.max; ++i) {
    double fi = Math.Sqrt(
      pow2(pal.ptr[i].b - src.b) +
      pow2(pal.ptr[i].g - src.g) +
      pow2(pal.ptr[i].r - src.r));
    if (fi < f_min) {
      best_color = i;
      f_min = fi;
    }
  } // for max pal
  return pal.ptr[best_color];
} // color_find_EUCLIDE

dRGB color_find_DIFFERENCE(dRGB src, Palette pal) {
  /* Проход по всем цветам палитры и вычисление схожести
  цвета с палитры с цветом картинки. Если current
  меньше, то цвета более схожи. */
  double total = 3.0;
  int index = 0;
  for (int i = 0; i < pal.max; ++i) {
    double diff = 
      Math.Abs(pal.ptr[i].b - src.b) +
      Math.Abs(pal.ptr[i].g - src.g) +
      Math.Abs(pal.ptr[i].r - src.r);
    if (total > diff) {
      total = diff;
      index = i;
    }
  }
  return pal.ptr[index];
} // color_find_DIFFERENCE

dRGB color_find_average(dRGB src, Palette pal) {
  double total = 1.0;
  int index = 0;
  for (int i = 0; i < pal.max; ++i) {
    var a = (pal.ptr[i].b + pal.ptr[i].g + pal.ptr[i].r) / 3.0;
    var b = (src.b + src.g + src.r) / 3.0;
    double diff = Math.Abs(a - b);
    if (total > diff) {
      total = diff;
      index = i;
    }
  }
  return pal.ptr[index];
} // color_find_average

dRGB color_find_only_red(dRGB src, Palette pal) {
  double total = 1.0;
  int index = 0;
  for (int i = 0; i < pal.max; ++i) {
    double diff = Math.Abs(pal.ptr[i].r - src.r);
    if (total > diff) {
      total = diff;
      index = i;
    }
  }
  return pal.ptr[index];
} // color_find_only_red

dRGB color_find_only_green(dRGB src, Palette pal) {
  double total = 1.0;
  int index = 0;
  for (int i = 0; i < pal.max; ++i) {
    double diff = Math.Abs(pal.ptr[i].g - src.g);
    if (total > diff) {
      total = diff;
      index = i;
    }
  }
  return pal.ptr[index];
} // color_find_only_green

dRGB color_find_only_blue(dRGB src, Palette pal) {
  double total = 1.0;
  int index = 0;
  for (int i = 0; i < pal.max; ++i) {
    double diff = Math.Abs(pal.ptr[i].b - src.b);
    if (total > diff) {
      total = diff;
      index = i;
    }
  }
  return pal.ptr[index];
} // color_find_only_blue

class CompareRed: IComparer<dRGB> {
  public int Compare(dRGB a, dRGB b) {
    if (a.r == b.r) return 0;
    else if (a.r < b.r) return -1;
    else return 1;
  }
} // CompareRed

class CompareGreen: IComparer<dRGB> {
  public int Compare(dRGB a, dRGB b) {
    if (a.g == b.g) return 0;
    else if (a.g < b.g) return -1;
    else return 1;
  }
} // CompareGreen

class CompareBlue: IComparer<dRGB> {
  public int Compare(dRGB a, dRGB b) {
    if (a.b == b.b) return 0;
    else if (a.b < b.b) return -1;
    else return 1;
  }
} // CompareBlue

// сортирует самый распространённый цветовой канал
void proc_for_median_cut(List<dRGB> pixlist) {
// определение диапазона каналов:
  double r_min = 0, r_max = 1;
  double g_min = 0, g_max = 1;
  double b_min = 0, b_max = 1;
  foreach (var pix in pixlist) {
    r_min = Math.Min(r_min, pix.r); r_max = Math.Max(r_max, pix.r);
    g_min = Math.Min(g_min, pix.g); g_max = Math.Max(g_max, pix.g);
    b_min = Math.Min(b_min, pix.b); b_max = Math.Max(b_max, pix.b);
  }
  double r_range = r_max - r_min;
  double g_range = g_max - g_min;
  double b_range = b_max - b_min;
  if (r_range > g_range && r_range > b_range) // сортировка по красному каналу
    pixlist.Sort(new CompareRed());
  else if (g_range > r_range && g_range > b_range) // сортировка по зелёному каналу
    pixlist.Sort(new CompareGreen());
  else // сортировка по синему каналу
    pixlist.Sort(new CompareBlue());
} // proc_for_median_cut

// делает оптимальную палитру в зависимости от часто встречающихся пикселей
void init_adaptive_pal(Palette pal, Img img, int color_count = 16) {
  pal.max = color_count;
  pal.ptr = new dRGB[color_count];
// сохранение пикселей в списке
  var pixlist = new List<dRGB>();
  for (int i = 0; i < img.X * img.Y; ++i)
    pixlist.Add(img.fast_get(i));
// просчитать границы списка для цветов
  var bound_list = new List<Tuple<int, int>>();
  proc_for_median_cut(pixlist);
  double mul = (double)pixlist.Count() / color_count;
  for (int pal_index = 0; pal_index < color_count; ++pal_index)
    pal.ptr[pal_index] = pixlist[(int)(pal_index * mul)];
} // init_adaptive_pal

// палитра определяется через m_palette
Palette init_palette(Img src) {
  Palette pal = new Palette();
  if (m_use_file) {
    // если загрузка из файла палитр:
    if (m_fname != "") {
      string[] lines = File.ReadAllLines(m_fname);
      int colors = 0;
      // узнать сколько цветов в файле палитры:
      foreach (string line in lines) {
        if (line[0] == ';') // пропуск коментов
          continue;
        ++colors;
      }
      if (colors == 0)
        return null;
      else {
        pal.max = colors;
        pal.ptr = new dRGB[colors];
      }
      // конвертирование цветов из текста в палитру
      int i = 0;
      foreach (string line in lines) {
        if (line[0] == ';') // пропуск коментов
          continue;
        pal.ptr[i] = str2col(line);
        ++i;
      }
      return pal;
    }
  } // use file?
  // выбирается встроенная палитра
  palette_e pla_mode;
  if (m_use_adaptive)
    pla_mode = palette_e.ADAPTIVE;
  else
    pla_mode = (palette_e)m_palette;
  switch (pla_mode) {
    case palette_e.ADAPTIVE: init_adaptive_pal(pal, src, m_col_count); break;
    case palette_e.BW: {
      // инициализация чб палитры:
      pal.max = 2;
      pal.ptr = new dRGB[pal.max];
      pal.ptr[0] = new dRGB(0xFF000000); // чёрный
      pal.ptr[1] = new dRGB(0xFFFFFFFF); // белый
      break;
    }
    case palette_e._3BIT:
    default: {
      // инициализация простейшей палитры:
      pal.max = 8;
      pal.ptr = new dRGB[pal.max];
      pal.ptr[0] = new dRGB(0xFF000000); // чёрный
      pal.ptr[1] = new dRGB(0xFFFF0000); // красный
      pal.ptr[2] = new dRGB(0xFF00FF00); // зелёный
      pal.ptr[3] = new dRGB(0xFFFFFF00); // жёлтый
      pal.ptr[4] = new dRGB(0xFF0000FF); // синий
      pal.ptr[5] = new dRGB(0xFFFF00FF); // фиолетовый
      pal.ptr[6] = new dRGB(0xFF00FFFF); // бирюзовый
      pal.ptr[7] = new dRGB(0xFFFFFFFF); // белый
      return pal;
    } // _3BIT
    case palette_e.MSX: {
      pal.max = 15;
      pal.ptr = new dRGB[pal.max];
      pal.ptr[0]  = new dRGB(0xFF000000);
      pal.ptr[1]  = new dRGB(0xFF3EB849);
      pal.ptr[2]  = new dRGB(0xFF74D07D);
      pal.ptr[3]  = new dRGB(0xFF5955E0);
      pal.ptr[4]  = new dRGB(0xFF8076F1);
      pal.ptr[5]  = new dRGB(0xFFB95E51);
      pal.ptr[6]  = new dRGB(0xFF65DBEF);
      pal.ptr[7]  = new dRGB(0xFFDB6559);
      pal.ptr[8]  = new dRGB(0xFFFF897D);
      pal.ptr[9]  = new dRGB(0xFFCCC35E);
      pal.ptr[10] = new dRGB(0xFFDED087);
      pal.ptr[11] = new dRGB(0xFF3AA241);
      pal.ptr[12] = new dRGB(0xFFB766B5);
      pal.ptr[13] = new dRGB(0xFFCCCCCC);
      pal.ptr[14] = new dRGB(0xFFFFFFFF);
      return pal;
    } // MSX
    case palette_e.ZX_SPECTRUM: {
      pal.max = 15;
      pal.ptr = new dRGB[pal.max];
      pal.ptr[0]  = new dRGB(0xFF000000);
      pal.ptr[1]  = new dRGB(0xFF001DC8);
      pal.ptr[2]  = new dRGB(0xFF0027FB);
      pal.ptr[3]  = new dRGB(0xFFD8240F);
      pal.ptr[4]  = new dRGB(0xFFFF3016);
      pal.ptr[5]  = new dRGB(0xFFD530C9);
      pal.ptr[6]  = new dRGB(0xFFFF3FFC);
      pal.ptr[7]  = new dRGB(0xFF00C721);
      pal.ptr[8]  = new dRGB(0xFF00F92C);
      pal.ptr[9]  = new dRGB(0xFF00C9CB);
      pal.ptr[10] = new dRGB(0xFF00FCFE);
      pal.ptr[11] = new dRGB(0xFFCEC927);
      pal.ptr[12] = new dRGB(0xFFFFFD33);
      pal.ptr[13] = new dRGB(0xFFCBCBCB);
      pal.ptr[14] = new dRGB(0xFFFFFFFF);
      return pal;
    } // ZX_SPECTRUM
    case palette_e.COMMODORE64: {
      pal.max = 16;
      pal.ptr = new dRGB[pal.max];
      pal.ptr[0]  = new dRGB(0xFF000000);
      pal.ptr[1]  = new dRGB(0xff626262);
      pal.ptr[2]  = new dRGB(0xff898989);
      pal.ptr[3]  = new dRGB(0xffadadad);
      pal.ptr[4]  = new dRGB(0xffffffff);
      pal.ptr[5]  = new dRGB(0xff9f4e44);
      pal.ptr[6]  = new dRGB(0xffcb7e75);
      pal.ptr[7]  = new dRGB(0xff6d5412);
      pal.ptr[8]  = new dRGB(0xffa1683c);
      pal.ptr[9]  = new dRGB(0xffc9d487);
      pal.ptr[10] = new dRGB(0xff9ae29b);
      pal.ptr[11] = new dRGB(0xff5cab5e);
      pal.ptr[12] = new dRGB(0xff6abfc6);
      pal.ptr[13] = new dRGB(0xff887ecb);
      pal.ptr[14] = new dRGB(0xff50459b);
      pal.ptr[15] = new dRGB(0xffa057a3);
      return pal;
    } // COMMODORE64
    case palette_e.MAC_16COL: {
      pal.max = 16;
      pal.ptr = new dRGB[pal.max];
      pal.ptr[0]  = new dRGB(0xFF000000);
      pal.ptr[1]  = new dRGB(0xFF663300);
      pal.ptr[2]  = new dRGB(0xFFDD0000);
      pal.ptr[3]  = new dRGB(0xFF006600);
      pal.ptr[4]  = new dRGB(0xFF00AA00);
      pal.ptr[5]  = new dRGB(0xFF330099);
      pal.ptr[6]  = new dRGB(0xFF0000CC);
      pal.ptr[7]  = new dRGB(0xFF444444);
      pal.ptr[8]  = new dRGB(0xFF0099FF);
      pal.ptr[9]  = new dRGB(0xFFFF0099);
      pal.ptr[10] = new dRGB(0xFF996633);
      pal.ptr[11] = new dRGB(0xFFFFFF00);
      pal.ptr[12] = new dRGB(0xFF888888);
      pal.ptr[13] = new dRGB(0xFFBBBBBB);
      pal.ptr[14] = new dRGB(0xFFFFFFFF);
      pal.ptr[15] = new dRGB(0xFFFF6600);
      return pal;
    } // MAC_16COL
    case palette_e.RGBI_3LEVEL: {
      pal.max = 27;
      pal.ptr = new dRGB[pal.max];
      pal.ptr[0]  = new dRGB(0xFF000000);
      pal.ptr[1]  = new dRGB(0xFF910000);
      pal.ptr[2]  = new dRGB(0xFFFF0000);
      pal.ptr[3]  = new dRGB(0xFF009100);
      pal.ptr[4]  = new dRGB(0xFF00FF00);
      pal.ptr[5]  = new dRGB(0xFF000091);
      pal.ptr[6]  = new dRGB(0xFF0000FF);
      pal.ptr[7]  = new dRGB(0xFF009191);
      pal.ptr[8]  = new dRGB(0xFF00FFFF);
      pal.ptr[9]  = new dRGB(0xFF910091);
      pal.ptr[10] = new dRGB(0xFFFF00FF);
      pal.ptr[11] = new dRGB(0xFF919100);
      pal.ptr[12] = new dRGB(0xFFFFFF00);
      pal.ptr[13] = new dRGB(0xFF919191);
      pal.ptr[14] = new dRGB(0xFFFFFFFF);
      pal.ptr[15] = new dRGB(0xFFFF9100);
      pal.ptr[16] = new dRGB(0xFF91FF00);
      pal.ptr[17] = new dRGB(0xFFFF0091);
      pal.ptr[18] = new dRGB(0xFF00FF91);
      pal.ptr[19] = new dRGB(0xFF9100FF);
      pal.ptr[20] = new dRGB(0xFF0091FF);
      pal.ptr[21] = new dRGB(0xFFFF9191);
      pal.ptr[22] = new dRGB(0xFFFFFF91);
      pal.ptr[23] = new dRGB(0xFF91FF91);
      pal.ptr[24] = new dRGB(0xFF91FFFF);
      pal.ptr[25] = new dRGB(0xFF9191FF);
      pal.ptr[26] = new dRGB(0xFFFF91FF);
      return pal;
    } // RGBI_3LEVEL
    case palette_e.RISCOS_16COL: {
      pal.max = 16;
      pal.ptr = new dRGB[pal.max];
      pal.ptr[0]  = new dRGB(0xFF000000);
      pal.ptr[1]  = new dRGB(0xFFDD0000);
      pal.ptr[2]  = new dRGB(0xFF558800);
      pal.ptr[3]  = new dRGB(0xFF00CC00);
      pal.ptr[4]  = new dRGB(0xFF004499);
      pal.ptr[5]  = new dRGB(0xFF00BBFF);
      pal.ptr[6]  = new dRGB(0xFFFFBB00);
      pal.ptr[7]  = new dRGB(0xFFEEEE00);
      pal.ptr[8]  = new dRGB(0xFF333333);
      pal.ptr[9]  = new dRGB(0xFF555555);
      pal.ptr[10] = new dRGB(0xFF777777);
      pal.ptr[11] = new dRGB(0xFF999999);
      pal.ptr[12] = new dRGB(0xFFBBBBBB);
      pal.ptr[13] = new dRGB(0xFFDDDDDD);
      pal.ptr[14] = new dRGB(0xFFFFFFFF);
      pal.ptr[15] = new dRGB(0xFFEEEEBB);
      return pal;
    } // RISCOS_16COL
  } // switch pal mode
  return pal;
} // init Palette

// применение палитры к цвету
dRGB accept_pal(dRGB col, Palette pal) {
  col = _colcor(col, dRGB.i2d(m_post_luma), m_post_contrast);
  switch ((find_e)m_find) {
    case find_e.BT2001: { col = color_find_BT2001(col, pal); break; }
    case find_e.BT601: { col = color_find_BT601(col, pal); break; }
    case find_e.EUCLIDE: { col = color_find_EUCLIDE(col, pal); break; }
    case find_e.AVERAGE: { col = color_find_average(col, pal); break; }
    case find_e.ONLY_RED: { col = color_find_only_red(col, pal); break; }
    case find_e.ONLY_GREEN: { col = color_find_only_green(col, pal); break; }
    case find_e.ONLY_BLUE: { col = color_find_only_blue(col, pal); break; }
    default:
    case find_e.DIFFERENCE: { col = color_find_DIFFERENCE(col, pal); break; }
  } // m_find
  return col;
}

void dither_ordered_2x2(Img dst, Palette pal) {
  double[,] matrix = new double[2,2] {
    {-0.25, 0.25},
    { 0.5,  0.0} };
  Img buffer = dst.make_copy();
  double mul = 1.0 / (double)pal.max * m_dither_mul;
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB col = buffer.fast_get(x, y);
      col += mul * matrix[x % 2, y % 2];
      col = accept_pal(col, pal);
      dst.fast_set(x, y, col);
    }
  }
  matrix = null;
} // dither_ordered_2x2

void dither_ordered_3x3(Img dst, Palette pal) {
  double[,] matrix = new double[3,3] {
    {3.0, 7.0, 5.0},
    {6.0, 1.0, 2.0},
    {9.0, 4.0, 8.0} };
  // улучшение матрицы по формуле:
  // Mpre(i,j) = (Mint(i,j)+1) / n^2 - 0.5
  for (int y = 0; y < 3; y++)
  for (int x = 0; x < 3; x++)
    matrix[x, y] = (matrix[x, y] / 9.0) - 0.5;
  Img buffer = dst.make_copy();
  double mul = 1.0 / (double)pal.max * m_dither_mul;
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB col = buffer.fast_get(x, y);
      col += mul * matrix[x % 3, y % 3];
      col = accept_pal(col, pal);
      dst.fast_set(x, y, col);
    }
  }
  matrix = null;
} // dither_ordered_3x3

void dither_ordered_4x4(Img dst, Palette pal) {
  const int msize = 4;
  const int msize_pow = msize * msize;
  double[,] matrix = new double[msize,msize] {
    { 0.0,  8.0,  2.0, 10.0},
    {12.0,  4.0, 14.0,  6.0},
    { 3.0, 11.0,  1.0,  9.0},
    {15.0,  7.0, 13.0,  5.0} };
  // улучшение матрицы по формуле:
  // Mpre(i,j) = (Mint(i,j)+1) / n^2 - 0.5
  for (int y = 0; y < msize; y++)
  for (int x = 0; x < msize; x++)
    matrix[x, y] = (matrix[x, y] / msize_pow) - 0.5;
  Img buffer = dst.make_copy();
  double mul = 1.0 / (double)pal.max * m_dither_mul;
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB col = buffer.fast_get(x, y);
      col += mul * matrix[x % msize, y % msize];
      col = accept_pal(col, pal);
      dst.fast_set(x, y, col);
    }
  }
  matrix = null;
} // dither_ordered_4x4

void dither_ordered_8x8(Img dst, Palette pal) {
  const int msize = 8;
  const int msize_pow = msize * msize;
  double[,] matrix = new double[msize,msize] {
    { 0.0, 48.0, 12.0, 60.0,  3.0, 51.0, 15.0, 63.0},
    {32.0, 16.0, 44.0, 28.0, 35.0, 19.0, 47.0, 31.0},
    { 8.0, 56.0,  4.0, 52.0, 11.0, 59.0,  7.0, 55.0},
    {40.0, 24.0, 36.0, 20.0, 43.0, 27.0, 39.0, 23.0},
    { 2.0, 50.0, 14.0, 62.0,  1.0, 49.0, 13.0, 61.0},
    {34.0, 18.0, 46.0, 30.0, 33.0, 17.0, 45.0, 29.0},
    {10.0, 58.0,  6.0, 54.0,  9.0, 57.0,  5.0, 53.0},
    {42.0, 26.0, 38.0, 22.0, 41.0, 25.0, 37.0, 21.0} };
  // улучшение матрицы по формуле:
  // Mpre(i,j) = (Mint(i,j)+1) / n^2 - 0.5
  for (int y = 0; y < msize; y++)
  for (int x = 0; x < msize; x++)
    matrix[x, y] = (matrix[x, y] / msize_pow) - 0.5;
  Img buffer = dst.make_copy();
  double mul = 1.0 / (double)pal.max * m_dither_mul;
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB col = buffer.fast_get(x, y);
      col += mul * matrix[x % msize, y % msize];
      col = accept_pal(col, pal);
      dst.fast_set(x, y, col);
    }
  }
  matrix = null;
} // dither_ordered_8x8

void dither_ordered_16x16(Img dst, Palette pal) {
  const int msize = 16;
  const int msize_pow = msize * msize;
  double[,] matrix = new double[msize,msize] {
    {255, 127, 223, 95 , 247, 119, 215, 87 , 253, 125, 221, 93 , 245, 117, 213, 85 },
    {63 , 191, 31 , 159, 55 , 183, 23 , 151, 61 , 189, 29 , 157, 53 , 181, 21 , 149},
    {207, 79 , 239, 111, 199, 71 , 231, 103, 205, 77 , 237, 109, 197, 69 , 229, 101},
    {15 , 143, 47 , 175, 7  , 135, 39 , 167, 13 , 141, 45 , 173, 5  , 133, 37 , 165},
    {243, 115, 211, 83 , 251, 123, 219, 91 , 241, 113, 209, 81 , 249, 121, 217, 89 },
    {51 , 179, 19 , 147, 59 , 187, 27 , 155, 49 , 177, 17 , 145, 57 , 185, 25 , 153},
    {195, 67 , 227, 99 , 203, 75 , 235, 107, 193, 65 , 225, 97 , 201, 73 , 233, 105},
    {3  , 131, 35 , 163, 11 , 139, 43 , 171, 1  , 129, 33 , 161, 9  , 137, 41 , 169},
    {252, 124, 220, 92 , 244, 116, 212, 84 , 254, 126, 222, 94 , 246, 118, 214, 86 },
    {60 , 188, 28 , 156, 52 , 180, 20 , 148, 62 , 190, 30 , 158, 54 , 182, 22 , 150},
    {204, 76 , 236, 108, 196, 68 , 228, 100, 206, 78 , 238, 110, 198, 70 , 230, 102},
    {12 , 140, 44 , 172, 4  , 132, 36 , 164, 14 , 142, 46 , 174, 6  , 134, 38 , 166},
    {240, 112, 208, 80 , 248, 120, 216, 88 , 242, 114, 210, 82 , 250, 122, 218, 90 },
    {48 , 176, 16 , 144, 56 , 184, 24 , 152, 50 , 178, 18 , 146, 58 , 186, 26 , 154},
    {192, 64 , 224, 96 , 200, 72 , 232, 104, 194, 66 , 226, 98 , 202, 74 , 234, 106},
    {0  , 128, 32 , 160, 8  , 136, 40 , 168, 2  , 130, 34 , 162, 10 , 138, 42 , 170} };
  // улучшение матрицы по формуле:
  // Mpre(i,j) = (Mint(i,j)+1) / n^2 - 0.5
  for (int y = 0; y < msize; y++)
  for (int x = 0; x < msize; x++)
    matrix[x, y] = (matrix[x, y] / msize_pow) - 0.5;
  Img buffer = dst.make_copy();
  double mul = 1.0 / (double)pal.max * m_dither_mul;
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB col = buffer.fast_get(x, y);
      col += mul * matrix[x % msize, y % msize];
      col = accept_pal(col, pal);
      dst.fast_set(x, y, col);
    }
  }
  matrix = null;
} // dither_ordered_16x16

// по m_error вычисляет ошибку дизеринга
dRGB dither_error(dRGB a, dRGB b) {
  switch ((error_e)m_error) {
    default:
    case error_e.diff: return a - b;
    case error_e.diff_sum: {
      double l = (a.r + a.g + a.b) - (b.r + b.g + b.b);
      return new dRGB(l, l, l);
    }
    case error_e.avg: {
      double l = (a.r + a.g + a.b) / 3.0 - (b.r + b.g + b.b) / 3.0;
      return new dRGB(l, l, l);
    }
  }
} // dither_error

void dither_stucki(Img dst, Palette pal) {
  Img buffer = dst.make_copy();
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB old_pixel = buffer.fast_get(x, y);
      dRGB new_pixel = accept_pal(old_pixel, pal);
      buffer.fast_set(x, y, new_pixel);
      dRGB q_error = dither_error(old_pixel, new_pixel) / (1.0 + m_dither_mul);
      buffer.set(x+1, y  , buffer.get(x+1, y  ) + q_error * (8.0/42.0));
      buffer.set(x+2, y  , buffer.get(x+2, y  ) + q_error * (4.0/42.0));

      buffer.set(x-2, y+1, buffer.get(x-2, y+1) + q_error * (2.0/42.0));
      buffer.set(x-1, y+1, buffer.get(x-1, y+1) + q_error * (4.0/42.0));
      buffer.set(x  , y+1, buffer.get(x  , y+1) + q_error * (8.0/42.0));
      buffer.set(x+1, y+1, buffer.get(x+1, y+1) + q_error * (4.0/42.0));
      buffer.set(x+2, y+1, buffer.get(x+2, y+1) + q_error * (2.0/42.0));

      buffer.set(x-2, y+2, buffer.get(x-2, y+2) + q_error * (1.0/42.0));
      buffer.set(x-1, y+2, buffer.get(x-1, y+2) + q_error * (2.0/42.0));
      buffer.set(x  , y+2, buffer.get(x  , y+2) + q_error * (4.0/42.0));
      buffer.set(x+1, y+2, buffer.get(x+1, y+2) + q_error * (2.0/42.0));
      buffer.set(x+2, y+2, buffer.get(x+2, y+2) + q_error * (1.0/42.0));
    }
  }
  dst.fast_read(buffer);
} // dither_stucki

void dither_line_v(Img dst, Palette pal) {
  for (int y = 0; y < dst.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < dst.X; x++) {
      var col = dst.fast_get(x, y);
      if (x % 2 == 1) {
        col -= 0.25 * m_dither_mul;
      } else {
        col += 0.25 * m_dither_mul;
      }
      dst.fast_set(x, y, accept_pal(col, pal));
    }
  }
} // dither_line_v

void dither_line_h(Img dst, Palette pal) {
  for (int y = 0; y < dst.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < dst.X; x++) {
      var col = dst.fast_get(x, y);
      if (y % 2 == 1) {
        col -= 0.25 * m_dither_mul;
      } else {
        col += 0.25 * m_dither_mul;
      }
      dst.fast_set(x, y, accept_pal(col, pal));
    }
  }
} // dither_line_h

void dither_floyd(Img dst, Palette pal) {
  Img buffer = dst.make_copy();
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB old_pixel = buffer.fast_get(x, y);
      dRGB new_pixel = accept_pal(old_pixel, pal);
      buffer.fast_set(x, y, new_pixel);
      dRGB q_error = dither_error(old_pixel, new_pixel) / (1.0 + m_dither_mul);
      buffer.set(x+1, y  , buffer.get(x+1, y  ) + q_error * (7.0/16.0));
      buffer.set(x-1, y+1, buffer.get(x-1, y+1) + q_error * (3.0/16.0));
      buffer.set(x  , y+1, buffer.get(x  , y+1) + q_error * (5.0/16.0));
      buffer.set(x+1, y+1, buffer.get(x+1, y+1) + q_error * (1.0/16.0));
    }
  }
  dst.fast_read(buffer);
} // dither_floyd

void dither_floyd_false(Img dst, Palette pal) {
  Img buffer = dst.make_copy();
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB old_pixel = buffer.fast_get(x, y);
      dRGB new_pixel = accept_pal(old_pixel, pal);
      buffer.fast_set(x, y, new_pixel);
      dRGB q_error = dither_error(old_pixel, new_pixel) / (1.0 + m_dither_mul);
      buffer.set(x+1, y  , buffer.get(x+1, y  ) + q_error * 3 * 0.125); // 0.125 = 1/8
      buffer.set(x+1, y+1, buffer.get(x+1, y+1) + q_error * 2 * 0.125);
      buffer.set(x  , y+1, buffer.get(x  , y+1) + q_error * 3 * 0.125);
    }
  }
  dst.fast_read(buffer);
} // dither_floyd_false

void dither_jarvis(Img dst, Palette pal) {
  Img buffer = dst.make_copy();
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB old_pixel = buffer.fast_get(x, y);
      dRGB new_pixel = accept_pal(old_pixel, pal);
      buffer.fast_set(x, y, new_pixel);
      dRGB q_error = dither_error(old_pixel, new_pixel) / (1.0 + m_dither_mul);
      buffer.set(x+1, y  , buffer.get(x+1, y  ) + q_error * (7.0/48.0));
      buffer.set(x+2, y  , buffer.get(x+2, y  ) + q_error * (5.0/48.0));

      buffer.set(x-2, y+1, buffer.get(x-2, y+1) + q_error * (3.0/48.0));
      buffer.set(x-1, y+1, buffer.get(x-1, y+1) + q_error * (5.0/48.0));
      buffer.set(x  , y+1, buffer.get(x  , y+1) + q_error * (7.0/48.0));
      buffer.set(x+1, y+1, buffer.get(x+1, y+1) + q_error * (5.0/48.0));
      buffer.set(x+2, y+1, buffer.get(x+2, y+1) + q_error * (3.0/48.0));

      buffer.set(x-2, y+2, buffer.get(x-2, y+2) + q_error * (1.0/48.0));
      buffer.set(x-1, y+2, buffer.get(x-1, y+2) + q_error * (3.0/48.0));
      buffer.set(x  , y+2, buffer.get(x  , y+2) + q_error * (5.0/48.0));
      buffer.set(x+1, y+2, buffer.get(x+1, y+2) + q_error * (3.0/48.0));
      buffer.set(x+2, y+2, buffer.get(x+2, y+2) + q_error * (1.0/48.0));
    }
  }
  dst.fast_read(buffer);
} // dither_jarvis

void dither_burkes(Img dst, Palette pal) {
  Img buffer = dst.make_copy();
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB old_pixel = buffer.fast_get(x, y);
      dRGB new_pixel = accept_pal(old_pixel, pal);
      buffer.fast_set(x, y, new_pixel);
      dRGB q_error = dither_error(old_pixel, new_pixel) / (1.0 + m_dither_mul);
      buffer.set(x+1, y  , buffer.get(x+1, y  ) + q_error * (8.0/32.0));
      buffer.set(x+2, y  , buffer.get(x+2, y  ) + q_error * (4.0/32.0));

      buffer.set(x-2, y+1, buffer.get(x-2, y+1) + q_error * (2.0/32.0));
      buffer.set(x-1, y+1, buffer.get(x-1, y+1) + q_error * (4.0/32.0));
      buffer.set(x  , y+1, buffer.get(x  , y+1) + q_error * (8.0/32.0));
      buffer.set(x+2, y+1, buffer.get(x+2, y+1) + q_error * (2.0/32.0));
      buffer.set(x+1, y+1, buffer.get(x+1, y+1) + q_error * (4.0/32.0));
    }
  }
  dst.fast_read(buffer);
} // dither_burkes

void dither_sierra3(Img dst, Palette pal) {
  Img buffer = dst.make_copy();
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB old_pixel = buffer.fast_get(x, y);
      dRGB new_pixel = accept_pal(old_pixel, pal);
      buffer.fast_set(x, y, new_pixel);
      dRGB q_error = dither_error(old_pixel, new_pixel) / (1.0 + m_dither_mul);
      buffer.set(x+1, y  , buffer.get(x+1, y  ) + q_error * (5.0/32.0));
      buffer.set(x+2, y  , buffer.get(x+2, y  ) + q_error * (3.0/32.0));
      
      buffer.set(x-2, y+1, buffer.get(x-2, y+1) + q_error * (2.0/32.0));
      buffer.set(x-1, y+1, buffer.get(x-1, y+1) + q_error * (4.0/32.0));
      buffer.set(x  , y+1, buffer.get(x  , y+1) + q_error * (5.0/32.0));
      buffer.set(x+1, y+1, buffer.get(x+1, y+1) + q_error * (4.0/32.0));
      buffer.set(x+2, y+1, buffer.get(x+2, y+1) + q_error * (2.0/32.0));
      buffer.set(x-1, y+1, buffer.get(x-1, y+1) + q_error * (2.0/32.0));

      buffer.set(x  , y+1, buffer.get(x  , y+1) + q_error * (3.0/32.0));
      buffer.set(x+1, y+1, buffer.get(x+1, y+1) + q_error * (2.0/32.0));
    }
  }
  dst.fast_read(buffer);
} // dither_sierra3

void dither_sierra2(Img dst, Palette pal) {
  Img buffer = dst.make_copy();
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB old_pixel = buffer.fast_get(x, y);
      dRGB new_pixel = accept_pal(old_pixel, pal);
      buffer.fast_set(x, y, new_pixel);
      dRGB q_error = dither_error(old_pixel, new_pixel) / (1.0 + m_dither_mul);
      buffer.set(x+1, y  , buffer.get(x+1, y  ) + q_error * (4.0/16.0));
      buffer.set(x+2, y  , buffer.get(x+2, y  ) + q_error * (3.0/16.0));

      buffer.set(x-2, y+1, buffer.get(x-2, y+1) + q_error * (1.0/16.0));
      buffer.set(x-1, y+1, buffer.get(x-1, y+1) + q_error * (2.0/16.0));
      buffer.set(x  , y+1, buffer.get(x  , y+1) + q_error * (3.0/16.0));
      buffer.set(x+1, y+1, buffer.get(x+1, y+1) + q_error * (2.0/16.0));
      buffer.set(x+2, y+1, buffer.get(x+2, y+1) + q_error * (1.0/16.0));
    }
  }
  dst.fast_read(buffer);
} // dither_sierra2

void dither_sierra2_4a(Img dst, Palette pal) {
  Img buffer = dst.make_copy();
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB old_pixel = buffer.fast_get(x, y);
      dRGB new_pixel = accept_pal(old_pixel, pal);
      buffer.fast_set(x, y, new_pixel);
      dRGB q_error = dither_error(old_pixel, new_pixel) / (1.0 + m_dither_mul);
      buffer.set(x+1, y  , buffer.get(x+1, y  ) + q_error * (2.0/4.0));
      buffer.set(x-1, y+1, buffer.get(x-1, y+1) + q_error * (1.0/4.0));
      buffer.set(x  , y+1, buffer.get(x  , y+1) + q_error * (1.0/4.0));
    }
  }
  dst.fast_read(buffer);
} // dither_sierra2_4a

void dither_atkinson(Img dst, Palette pal) {
  Img buffer = dst.make_copy();
  for (int y = 0; y < buffer.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < buffer.X; x++) {
      dRGB old_pixel = buffer.fast_get(x, y);
      dRGB new_pixel = accept_pal(old_pixel, pal);
      buffer.fast_set(x, y, new_pixel);
      dRGB q_error = dither_error(old_pixel, new_pixel) / (1.0 + m_dither_mul);
      buffer.set(x+1, y  , buffer.get(x+1, y  ) + q_error * 0.125);
      buffer.set(x+2, y  , buffer.get(x+2, y  ) + q_error * 0.125);
      buffer.set(x-1, y+1, buffer.get(x-1, y+1) + q_error * 0.125);
      buffer.set(x  , y+1, buffer.get(x  , y+1) + q_error * 0.125);
      buffer.set(x+1, y+1, buffer.get(x+1, y+1) + q_error * 0.125);
      buffer.set(x  , y+2, buffer.get(x  , y+2) + q_error * 0.125);
    }
  }
  dst.fast_read(buffer);
} // dither_atkinson

void dither_random(Img dst, Palette pal) {
  Random generator = new Random();
  for (int y = 0; y < dst.Y; y++) {
    if (IsCancelRequested) return;
    for (int x = 0; x < dst.X; x++) {
      var col = dst.fast_get(x, y);
      col.r = col.r > (generator.NextDouble() * m_dither_mul) ? 1.0 : 0.0;
      col.g = col.g > (generator.NextDouble() * m_dither_mul) ? 1.0 : 0.0;
      col.b = col.b > (generator.NextDouble() * m_dither_mul) ? 1.0 : 0.0;
      dst.fast_set(x, y, accept_pal(col, pal));
    }
  }
} // dither_random

void add_alpha(Surface dst, Surface src) {
  var X = src.Width;
  var Y = src.Height;
  for (int y = 0; y < Y; y++)
  for (int x = 0; x < X; x++) {
    var src_col = src[x,y].Bgra;
    var dst_col = dst[x,y].Bgra;
    var result = new ColorBgra();
    result.A = (byte)((src_col >> 24) & 0xFF);
    result.R = (byte)((dst_col >> 16) & 0xFF);
    result.G = (byte)((dst_col >> 8) & 0xFF);
    result.B = (byte)((dst_col >> 0) & 0xFF);
    dst[x,y] = result;    
  }
}

void PreRender(Surface dst, Surface src) {}

void Render(Surface dst, Surface src, Rectangle rect) {
  Img img_src = new Img(src);
  img_src.colcor(dRGB.i2d(m_luma), m_contrast);
  Palette pal = init_palette(img_src);
  if (pal == null)
    return;
  switch ((dither_e)m_dither) {
    default:
    case dither_e.NONE_DITHER: { // NONE_DITHER
      for (int i = 0; i < img_src.X * img_src.Y; ++i)
        img_src.buffer[i] = accept_pal(img_src.buffer[i], pal);
      break;
    }
    case dither_e.ORDER2X2: dither_ordered_2x2(img_src, pal); break;
    case dither_e.ORDER3X3: dither_ordered_3x3(img_src, pal); break;
    case dither_e.ORDER4X4: dither_ordered_4x4(img_src, pal); break;
    case dither_e.ORDER8X8: dither_ordered_8x8(img_src, pal); break;
    case dither_e.ORDER16X16: dither_ordered_16x16(img_src, pal); break;
    case dither_e.STUCKI: dither_stucki(img_src, pal); break;
    case dither_e.LINEV: dither_line_v(img_src, pal); break;
    case dither_e.LINEH: dither_line_h(img_src, pal); break;
    case dither_e.FLOYD: dither_floyd(img_src, pal); break;
    case dither_e.FLOYD_FALSE: dither_floyd_false(img_src, pal); break;
    case dither_e.JARVIS: dither_jarvis(img_src, pal); break;
    case dither_e.BURKES: dither_burkes(img_src, pal); break;
    case dither_e.SIERRA3: dither_sierra3(img_src, pal); break;
    case dither_e.SIERRA2: dither_sierra2(img_src, pal); break;
    case dither_e.SIERRA2_4A: dither_sierra2_4a(img_src, pal); break;
    case dither_e.ATKINSON: dither_atkinson(img_src, pal); break;
    case dither_e.RANDOM: dither_random(img_src, pal); break;
  } // switch m_dither
  img_src.write(dst);
  if (m_use_alpha)
    add_alpha(dst, src);
} // Render