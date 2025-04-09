using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace USGReportApp
{
    public partial class PrintPreviewWindow : Window
    {
        public PrintPreviewWindow(string reportText, string letterheadImagePath)
        {
            InitializeComponent();
            LoadLetterhead(letterheadImagePath);
            SetFormattedReportText(reportText);
        }

        private void LoadLetterhead(string imagePath)
        {
            if (File.Exists(imagePath))
            {
                imgLetterhead.Source = new BitmapImage(new Uri(imagePath));
            }
            else
            {
                MessageBox.Show("Letterhead image not found.");
            }
        }

        private void SetFormattedReportText(string content)
        {
            var flowDoc = new FlowDocument
            {
                LineHeight = 1.0,
                PagePadding = new Thickness(0),
                Background = Brushes.Transparent
            };

            string[] lines = content.Split(new[] { "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var paragraph = new Paragraph
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Left,
                    LineHeight = 1.0
                };

                bool isHeading = line.EndsWith(":") ||
                           line.Contains("Findings") ||
                           line.Contains("Impression") ||
                           line.Contains("Recommendations") ||
                           line.Contains("Abnormal Findings") ||
                           line.Contains("Patient Information") ||
                           line.Contains("Referring Physician") ||
                           line.Contains("Reporting Radiologist");

                var run = new Run(line);

                if (isHeading)
                {
                    run.FontWeight = FontWeights.Bold;
                    run.FontSize = 12;
                    run.Foreground = Brushes.Black;
                }
                else
                {
                    run.FontSize = 10;
                    run.Foreground = Brushes.Black;
                }

                paragraph.Inlines.Add(run);
                flowDoc.Blocks.Add(paragraph);
            }

            richTextPreview.Document = flowDoc;
        }

        private void ClosePreview_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
