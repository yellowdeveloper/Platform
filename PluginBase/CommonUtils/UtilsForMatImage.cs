using OpenCvSharp;

namespace PluginBase.CommonUtils
{
    public static class UtilsForMatImage
    {
        public static Rect DrawTextWithBox<TClassArray>(Mat frame, Scalar rectColor, Scalar textColor, TClassArray cls, int prob, OpenCvSharp.Rect box, List<Rect> textBoxs)
        {
            string text = $"class: {cls.ToString()}  prob: {prob}";
            var font = HersheyFonts.Italic;
            double font_scale = 0.8;
            int thickness = 2;

            Size text_size = Cv2.GetTextSize(text, font, font_scale, thickness, out int baseline);
            var coord = new Point(box.X - 1, box.Y - 1);

            if (box.Y - text_size.Height < 0)
            {
                DebugLogger.Log(3, $"[WARN] Text Box Out of Bound Found! Adjusting ...");
                coord.Y = box.Y + text_size.Height + 1;
            }
            if (box.X + text_size.Width > 640)
            {
                DebugLogger.Log(3, $"[WARN] Text Box Out of Bound Found! Adjusting ...");
                coord.X = box.X - ((box.X + text_size.Width) - 640);
            }

            Rect background_rect = new Rect(
                coord.X,
                coord.Y - text_size.Height - baseline,
                text_size.Width,
                text_size.Height + 1 * baseline
                );

            background_rect = AvoidTextBoxIntersection(background_rect, textBoxs);
            coord.X = background_rect.X;
            coord.Y = background_rect.Y + text_size.Height;

            Cv2.Rectangle(frame, background_rect, rectColor, -1);
            Cv2.PutText(frame, text, coord, font, font_scale, textColor, thickness, LineTypes.AntiAlias);

            DebugLogger.Log(3, $"[DEBUG] Text Box Drawing Completed");

            return background_rect;
        }

        private static Rect AvoidTextBoxIntersection(Rect text_box, List<Rect> textBoxs)
        {
            if (textBoxs.Count == 0) return text_box;

            bool is_intersect = false;

            do
            {
                is_intersect = false;
                foreach (var box in textBoxs)
                {
                    if (text_box.IntersectsWith(box))
                    {
                        DebugLogger.Log(3, $"[DEBUG] Text Box Intersection Found! Avoiding ...");
                        text_box.Y = box.Bottom + 3;
                        is_intersect = true;
                        break;
                    }
                }
            } while (is_intersect);
            return text_box;
        }

        public static bool CheckIfRegionWhite(Mat frame, Rect region)
        {
            using (Mat refRect = frame[region])
            {
                Scalar meanColor = Cv2.Mean(refRect);

                double meanB = meanColor.Val0;
                double meanG = meanColor.Val1;
                double meanR = meanColor.Val2;

                DebugLogger.Log(3, $"[DEBUG] RefBox Mean Color - B:{meanB:F1}, G:{meanG:F1}, R:{meanR:F1}");

                double maxColor = Math.Max(meanR, Math.Max(meanG, meanB));
                double minColor = Math.Min(meanR, Math.Min(meanG, meanB));

                if (meanB < 110 || meanG < 110 || meanR < 110)
                {
                    DebugLogger.Log(3, $"[ERROR] ERROR!! REF REGION IS TOO DARK!");
                    return false;
                }

                if (maxColor - minColor > 35)
                {
                    DebugLogger.Log(3, $"[ERROR] ERROR!! REF REGION IS NOT WHITE!!");
                    return false;
                }

                return true;
            }
        }
    }
}
