﻿using Modules;
using SharpDX;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using Tesseract;

namespace Overlay.Objects
{
    internal class TessOCR : Singleton<TessOCR>
    {
        private TessOCR() { }

        public Bitmap ImageCrop(Bitmap rawImage, Rectangle rect)
        {
            Rectangle destRect = new Rectangle(System.Drawing.Point.Empty, rect.Size);

            Bitmap cropImage = new Bitmap(destRect.Width, destRect.Height);
            using (var graphics = Graphics.FromImage(cropImage))
            {
                graphics.DrawImage(rawImage, destRect, rect, GraphicsUnit.Pixel);
            }
            return cropImage;
        }

        public delegate void TessOCRHandler(Bitmap image);
        public event TessOCRHandler Processed;

        public string ReadFromItem(Bitmap src)
        {
            string ret = string.Empty;

            src = new Bitmap(src, src.Width * 3, src.Height * 3);
            src = this.ItemLocReadFilter(src);
            
            using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.TesseractOnly))
            {
                engine.SetVariable("tessedit_char_whitelist", "0123456789,()");
                Pix pix = Pix.LoadFromMemory(this.ImageToByte(src));
                var result = engine.Process(pix);
                ret = result.GetText();

                ret = this.ExtractCoordinates(ret);
            }
            return ret;
        }

        public string ReadFromMap(Bitmap src)
        {
            string result = string.Empty;
            try
            {
                //이미지의 크기를 두배로 키움
                Bitmap target = new Bitmap(src, src.Width * 2, src.Height * 2);
                Bitmap filter = this.MapLocReadFilter_unsafe(target);
                Bitmap dilate = this.DilateImage_unsafe(filter, 2);
                //filter.Save("C:\\Users\\Administrator\\Desktop\\Resources\\filter.bmp");
                //dilate.Save("C:\\Users\\Administrator\\Desktop\\Resources\\dilate.bmp");
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.TesseractOnly))
                {
                    engine.SetVariable("tessedit_char_whitelist", "0123456789,()");
                    Pix pix = Pix.LoadFromMemory(this.ImageToByte(dilate));
                    var ret = engine.Process(pix);
                    //Page.GetText() 메모리 누수 발견
                    //Nuget Package : Tesseract 5.2.0 version
                    result = ret.GetText();

                    ret.Dispose();
                    pix.Dispose();
                    engine.Dispose();
                }

                dilate?.Dispose();
                filter?.Dispose();
                target?.Dispose();
            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                GC.Collect();
            }


            return result;
        }

        private unsafe Bitmap MapLocReadFilter_unsafe(Bitmap image)
        {
            // 비트맵 포맷에 따라 PixelFormat을 설정합니다.
            BitmapData bitmapData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadWrite,
                image.PixelFormat);

            // 픽셀 데이터에 대한 포인터를 얻습니다.
            byte* scan0 = (byte*)bitmapData.Scan0.ToPointer();

            for (int y = 0; y < bitmapData.Height; y++)
            {
                for (int x = 0; x < bitmapData.Width; x++)
                {
                    // 픽셀 위치를 계산합니다.
                    byte* cel = scan0 + y * bitmapData.Stride + x * 4;
                    //Color cel = image.GetPixel(x, y);

                    //    if ((cel[2] > 50 && cel[2] < 110) &&
                    //(cel[1] > 240 && cel[1] < 255) &&
                    //    (cel[0] > 200 && cel[0] < 220))
                    if ((cel[2] > 40 && cel[2] < 120) && (cel[1] > 230 && cel[1] < 255) && (cel[0] > 190 && cel[0] < 230))
                    {
                        cel[0] = 0;
                        cel[1] = 0;
                        cel[2] = 0;
                        //cel[3] = 255;
                    }
                    else
                    {
                        cel[0] = 255;
                        cel[1] = 255;
                        cel[2] = 255;
                        //cel[3] = 255;
                    }
                }
            }

            image.UnlockBits(bitmapData);
            image.MakeTransparent();
            return image;
        }

        private Bitmap ItemLocReadFilter(Bitmap image)
        {
            Bitmap darkenedImage = new Bitmap(image.Width, image.Height);

            // 모든 픽셀에 대해 어두운 필터 적용
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color cel = image.GetPixel(x, y);

                    if (cel.R <= 150 && cel.G <= 150 && cel.B <= 150)
                    {
                        Color color = Color.FromArgb(255, 255, 255);
                        darkenedImage.SetPixel(x, y, color);
                    }
                    else
                    {
                        Color color = Color.FromArgb(0, 0, 0);
                        darkenedImage.SetPixel(x, y, color);
                    }
                }
            }

            return darkenedImage;
        }

        public unsafe Bitmap ItemLocReadFilter_unsafe(Bitmap image)
        {
            // 비트맵 포맷에 따라 PixelFormat을 설정합니다.
            BitmapData bitmapData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadWrite,
                image.PixelFormat);

            // 픽셀 데이터에 대한 포인터를 얻습니다.
            byte* scan0 = (byte*)bitmapData.Scan0.ToPointer();

            for (int y = 0; y < bitmapData.Height; y++)
            {
                for (int x = 0; x < bitmapData.Width; x++)
                {
                    // 픽셀 위치를 계산합니다.
                    byte* cel = scan0 + y * bitmapData.Stride + x * 4;

                    if (cel[2] <= 150 && cel[1] <= 150 && cel[0] <= 150)
                    {
                        cel[0] = 255;
                        cel[1] = 255;
                        cel[2] = 255;
                        //Color color = Color.FromArgb(255, 255, 255);
                        //darkenedImage.SetPixel(x, y, color);
                    }
                    else
                    {
                        cel[0] = 0;
                        cel[1] = 0;
                        cel[2] = 0;
                        //Color color = Color.FromArgb(0, 0, 0);
                        //darkenedImage.SetPixel(x, y, color);
                    }
                }
            }

            image.UnlockBits(bitmapData);
            image.MakeTransparent();
            return image;
        }

        private Bitmap MedianFilter(Bitmap original, int kernelSize)
        {
            Bitmap result = new Bitmap(original.Width, original.Height);

            // 필터 크기의 반을 계산
            int radius = kernelSize / 2;

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    // 주변 픽셀 값 리스트
                    List<int> values = new List<int>();

                    // 주변 픽셀 값 가져오기
                    for (int i = -radius; i <= radius; i++)
                    {
                        for (int j = -radius; j <= radius; j++)
                        {
                            int newX = x + i;
                            int newY = y + j;

                            // 이미지 경계 내에 있는 경우만 처리
                            if (newX >= 0 && newX < original.Width && newY >= 0 && newY < original.Height)
                            {
                                Color pixelColor = original.GetPixel(newX, newY);
                                int grayscaleValue = (int)(0.299 * pixelColor.R + 0.587 * pixelColor.G + 0.114 * pixelColor.B);
                                values.Add(grayscaleValue);
                            }
                        }
                    }

                    // 주변 픽셀 값 정렬
                    values.Sort();

                    // 중간값 설정
                    int medianValue = values[values.Count / 2];

                    // 결과 이미지에 중간값 설정
                    result.SetPixel(x, y, Color.FromArgb(medianValue, medianValue, medianValue));
                }
            }

            return result;
        }

        private unsafe Bitmap DilateImage_unsafe(Bitmap original, int dilationSize = 4)
        {
            Bitmap result = new Bitmap(original.Width, original.Height);

            // 원본 이미지 Lock
            BitmapData originalData = original.LockBits(
                new Rectangle(0, 0, original.Width, original.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            // 결과 이미지 Lock
            BitmapData resultData = result.LockBits(
                new Rectangle(0, 0, result.Width, result.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);


            int bytesPerPixel = Image.GetPixelFormatSize(original.PixelFormat) / 8;
            byte* ptrOriginal = (byte*)originalData.Scan0;
            byte* ptrResult = (byte*)resultData.Scan0;

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    byte* currentPixel = ptrOriginal + (y * originalData.Stride) + (x * bytesPerPixel);
                    byte blue = currentPixel[0];
                    byte green = currentPixel[1];
                    byte red = currentPixel[2];
                    byte alpha = currentPixel[3];

                    if (red == 0 && green == 0 && blue == 0) // 검은색 픽셀인 경우
                    {
                        for (int i = -dilationSize; i <= dilationSize; i++)
                        {
                            for (int j = -dilationSize; j <= dilationSize; j++)
                            {
                                int newX = x + i;
                                int newY = y + j;

                                if (newX >= 0 && newX < original.Width && newY >= 0 && newY < original.Height)
                                {
                                    byte* targetPixel = ptrResult + (newY * resultData.Stride) + (newX * bytesPerPixel);
                                    targetPixel[0] = 0; // Blue
                                    targetPixel[1] = 0; // Green
                                    targetPixel[2] = 0; // Red
                                    targetPixel[3] = alpha; // Alpha
                                }
                            }
                        }
                    }
                    else // 흰색 픽셀인 경우
                    {
                        byte* targetPixel = ptrResult + (y * resultData.Stride) + (x * bytesPerPixel);
                        targetPixel[0] = 255; // Blue
                        targetPixel[1] = 255; // Green
                        targetPixel[2] = 255; // Red
                        targetPixel[3] = alpha; // Alpha
                    }
                }
            }


            // 이미지의 잠금을 해제
            original.UnlockBits(originalData);
            result.UnlockBits(resultData);

            return result;
        }

        private byte[] ImageToByte(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }
        public string ExtractCoordinates(string text)
        {
            // 정규 표현식 패턴: 괄호 안의 숫자 쌍을 찾습니다.
            var mat = Regex.Match(text, @"\b(\d+)\s*[, ]\s*(\d+)\b");
            return (mat.Success ? mat.Value : string.Empty);
        }
    }
}
