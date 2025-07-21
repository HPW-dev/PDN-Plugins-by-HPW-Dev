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
#endregion

unsafe void PreRender(Surface dst, Surface src) {}

unsafe void Render(Surface dst, Surface src, Rectangle rect) {
    for (int y = rect.Top; y < rect.Bottom; y++) {
        if (IsCancelRequested) return;
        ColorBgra* srcPtr = src.GetPointPointerUnchecked(rect.Left, y);
        ColorBgra* dstPtr = dst.GetPointPointerUnchecked(rect.Left, y);
        
        for (int x = rect.Left; x < rect.Right; x++) {
            ColorBgra SrcPixel = *srcPtr;
            ColorBgra DstPixel = *dstPtr;

            ColorBgra CurrentPixel = SrcPixel;

            // TODO: Add additional pixel processing code here


            *dstPtr = CurrentPixel;
            srcPtr++;
            dstPtr++;
        }
    }
}

