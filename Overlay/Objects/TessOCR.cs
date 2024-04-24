using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            //src = this.DilateImage(src, false, 1);


            //string pathDir = Path.Combine(Directory.GetCurrentDirectory(), "ItemLog");
            //if (Directory.Exists(pathDir) == false)
            //{
            //    Directory.CreateDirectory(pathDir);
            //}

            using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.TesseractOnly))
            {
                engine.SetVariable("tessedit_char_whitelist", "0123456789,()");
                Pix pix = Pix.LoadFromMemory(this.ImageToByte(src));
                var result = engine.Process(pix);
                ret = result.GetText();

                ret = this.ExtractCoordinates(ret);

                //string logName = string.Format("[{0}{1}{2}.{3}]-[RET {4}].bmp",
                //    DateTime.Now.Hour,
                //    DateTime.Now.Minute,
                //    DateTime.Now.Second,
                //    DateTime.Now.Millisecond,
                //    ret);

                //string pathLog = Path.Combine(pathDir, logName);
                //src.Save(pathLog);
            }
            return ret;
        }

        public string ReadFromMap(Bitmap src)
        {
            //이미지의 크기를 두배로 키움
            src = new Bitmap(src, src.Width * 5, src.Height * 5);
            src = this.MapLocReadFilter(src);
            src = this.DilateImage(src, false, 4);

            using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.TesseractOnly))
            {
                engine.SetVariable("tessedit_char_whitelist", "0123456789,()");
                Pix pix = Pix.LoadFromMemory(this.ImageToByte(src));
                var result = engine.Process(pix);
                return result.GetText();
            }

        }

        private Bitmap ConvertToGrayScale(Bitmap image)
        {
            Bitmap grayImage = new Bitmap(image.Width, image.Height);

            // 모든 픽셀에 대해 그레이스케일로 변환
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);

                    // R, G, B 값을 평균하여 그레이스케일 값 계산
                    int grayValue = (int)((pixel.R + pixel.G + pixel.B) / 3.0);

                    // 새로운 색상으로 픽셀 설정
                    Color newPixel = Color.FromArgb(grayValue, grayValue, grayValue);
                    grayImage.SetPixel(x, y, newPixel);
                }
            }

            return grayImage;
        }

        private Bitmap MapLocReadFilter(Bitmap image)
        {
            Bitmap darkenedImage = new Bitmap(image.Width, image.Height);

            // 모든 픽셀에 대해 어두운 필터 적용
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color cel = image.GetPixel(x, y);
                    if ((cel.R > 50 && cel.R < 110) &&
                    (cel.G > 240 && cel.G < 255) &&
                        (cel.B > 200 && cel.B < 220))
                    {
                        Color color = Color.FromArgb(0, 0, 0);
                        darkenedImage.SetPixel(x, y, color);
                    }
                }
            }

            return darkenedImage;
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

        // 이미지에 어두운 필터 적용하는 메서드
        private Bitmap ApplyDarkFilter(Bitmap image, int darknessLevel)
        {
            Bitmap darkenedImage = new Bitmap(image.Width, image.Height);

            // 모든 픽셀에 대해 어두운 필터 적용
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);

                    // 픽셀의 RGB 값을 어두운 정도만큼 감소
                    int red = Math.Max(pixel.R - darknessLevel, 0);
                    int green = Math.Max(pixel.G - darknessLevel, 0);
                    int blue = Math.Max(pixel.B - darknessLevel, 0);

                    // 새로운 색상으로 픽셀 설정
                    Color newPixel = Color.FromArgb(red, green, blue);
                    darkenedImage.SetPixel(x, y, newPixel);
                }
            }

            return darkenedImage;
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

        public Bitmap GammaCorrection(Bitmap image, float gamma)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixelColor = image.GetPixel(x, y);
                    int r = (int)(255 * Math.Pow(pixelColor.R / 255.0, 1 / gamma));
                    int g = (int)(255 * Math.Pow(pixelColor.G / 255.0, 1 / gamma));
                    int b = (int)(255 * Math.Pow(pixelColor.B / 255.0, 1 / gamma));
                    result.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }

            return result;
        }

        private Bitmap DilateImage(Bitmap original, bool white = true, int dilationSize = 4)
        {
            // 팽창할 크기 정의

            Bitmap result = new Bitmap(original.Width, original.Height);

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    // 현재 픽셀의 색상을 가져옴
                    Color pixelColor = original.GetPixel(x, y);
                    if (white == true)
                    {
                        // 현재 픽셀이 흰색인 경우 주변의 픽셀도 흰색으로 설정
                        if (pixelColor.ToArgb() == Color.White.ToArgb())
                        {
                            for (int i = -dilationSize; i <= dilationSize; i++)
                            {
                                for (int j = -dilationSize; j <= dilationSize; j++)
                                {
                                    int newX = x + i;
                                    int newY = y + j;

                                    // 이미지 경계 내에 있는 경우만 처리
                                    if (newX >= 0 && newX < original.Width && newY >= 0 && newY < original.Height)
                                    {
                                        result.SetPixel(newX, newY, Color.White);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 현재 픽셀이 검은색인 경우 검은색으로 설정
                            result.SetPixel(x, y, Color.Black);
                        }
                    }
                    else
                    {
                        // 현재 픽셀이 흰색인 경우 주변의 픽셀도 흰색으로 설정
                        if (pixelColor.ToArgb() == Color.Black.ToArgb())
                        {
                            for (int i = -dilationSize; i <= dilationSize; i++)
                            {
                                for (int j = -dilationSize; j <= dilationSize; j++)
                                {
                                    int newX = x + i;
                                    int newY = y + j;

                                    // 이미지 경계 내에 있는 경우만 처리
                                    if (newX >= 0 && newX < original.Width && newY >= 0 && newY < original.Height)
                                    {
                                        result.SetPixel(newX, newY, Color.Black);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 현재 픽셀이 검은색인 경우 검은색으로 설정
                            result.SetPixel(x, y, Color.White);
                        }
                    }
                }
            }

            return result;
        }

        // 이미지 색상 반전 함수
        private Bitmap InvertColors(Bitmap original)
        {
            Bitmap result = new Bitmap(original.Width, original.Height);

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color pixelColor = original.GetPixel(x, y);

                    // 픽셀의 RGB 값을 반전시킴
                    Color invertedColor = Color.FromArgb(255 - pixelColor.R, 255 - pixelColor.G, 255 - pixelColor.B);

                    // 결과 이미지에 새로운 색상 설정
                    result.SetPixel(x, y, invertedColor);
                }
            }

            return result;
        }

        private Bitmap BinarizeImage(Bitmap originalImage, int threshold)
        {
            Bitmap binarizedImage = new Bitmap(originalImage.Width, originalImage.Height);

            // 각 픽셀의 밝기를 확인하여 임계값 이상인지 판별하여 흰색 또는 검은색으로 설정
            for (int y = 0; y < originalImage.Height; y++)
            {
                for (int x = 0; x < originalImage.Width; x++)
                {
                    Color pixelColor = originalImage.GetPixel(x, y);
                    int brightness = (int)(0.299 * pixelColor.R + 0.587 * pixelColor.G + 0.114 * pixelColor.B);

                    Color newColor = brightness > threshold ? Color.White : Color.Black;
                    binarizedImage.SetPixel(x, y, newColor);
                }
            }

            return binarizedImage;
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
