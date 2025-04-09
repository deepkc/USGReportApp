using System;
using System.Net.Http;
using System.Text;
using System.Speech.Recognition;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Windows.Input;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.Win32;
using static System.Net.Mime.MediaTypeNames;
using Document = iTextSharp.text.Document;
using System.IO;
using System.Windows.Documents;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PageSize = iTextSharp.text.PageSize;  // Alias iTextSharp's PageSize class
using Font = iTextSharp.text.Font;
using Paragraph = iTextSharp.text.Paragraph;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Net.Mail;





namespace USGReportApp
{
    public partial class MainWindow : Window
    {
        private const string apiKey = "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"; // Replace with your OpenAI API key
        private SpeechRecognitionEngine speechRecognitionEngine;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSpeechRecognition();
        }

        private void InitializeSpeechRecognition()
        {
            try
            {
                speechRecognitionEngine = new SpeechRecognitionEngine();

                // Set the input to the default microphone
                speechRecognitionEngine.SetInputToDefaultAudioDevice();

                // Register event handlers for speech recognition events
                speechRecognitionEngine.SpeechRecognitionRejected += SpeechRecognitionRejected;
                speechRecognitionEngine.AudioLevelUpdated += SpeechAudioLevelUpdated;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing speech recognition: {ex.Message}");
            }
        }

        private void btnStartListening_Click(object sender, RoutedEventArgs e)
        {

            // Start listening for control commands (like "Edit report", "Print report")
            StartListeningForCommands();
        }

        private void StartListeningForCommands()
        {
            try
            {
                // Remove any previous grammar related to findings
                speechRecognitionEngine.SpeechRecognized -= FindingsSpeechRecognized;
                speechRecognitionEngine.SpeechRecognized -= ControlCommandsSpeechRecognized; // Remove previous handlers

                // Define commands for controlling the app (Edit, Print, Preview)
                Choices commands = new Choices(new string[] {
            "Generate report",
            "Edit report",
            "Print report",
            "Save report",
            "Preview report",
            "Stop listening"
        });

                Grammar grammar = new Grammar(new GrammarBuilder(commands));

                // Load the grammar for controlling commands
                speechRecognitionEngine.LoadGrammar(grammar);

                // Set up event handler to handle control commands
                speechRecognitionEngine.SpeechRecognized += ControlCommandsSpeechRecognized; // Add only once

                // Start recognizing the speech commands
                speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);

                MessageBox.Show("Now listening for commands like 'Edit report', 'Print report', or 'Preview report'.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting command listening: {ex.Message}");
            }
        }


        private DateTime lastCommandTime = DateTime.MinValue;

        private void ControlCommandsSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            try
            {
                // Ensure a minimum time gap (e.g., 1 second) between repeated commands
                if ((DateTime.Now - lastCommandTime).TotalSeconds < 1)
                {
                    return; // Ignore duplicate commands within 1 second
                }
                lastCommandTime = DateTime.Now; // Update last command time

                switch (e.Result.Text)
                {
                    case "Generate report":
                        btnGenerateReport_Click(null, null);
                        break;
                    case "Edit report":
                        txtFindings.Focus();
                        StartListeningForFindings();
                        break;
                    case "Print report":
                        btnPrintReport_Click(null, null);
                        break;
                    case "Save report":
                        btnSaveReport_Click(null, null);
                        break;
                    case "Preview report":
                        btnPreviewReport_Click(null, null);
                        break;
                    case "Stop listening":
                        StopListening();
                        break;
                    default:
                        MessageBox.Show($"Command not recognized: {e.Result.Text}");
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing command: {ex.Message}");
            }
        }



        private void StartListeningForFindings()
        {
            try
            {
                // Remove command grammar and load dictation grammar for findings
                speechRecognitionEngine.SpeechRecognized -= ControlCommandsSpeechRecognized;

                // Define dictation grammar for findings input
                GrammarBuilder dictationGrammarBuilder = new GrammarBuilder();
                dictationGrammarBuilder.AppendDictation();
                Grammar dictationGrammar = new Grammar(dictationGrammarBuilder);

                speechRecognitionEngine.LoadGrammar(dictationGrammar);

                // Start recognizing dictation (findings)
                speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);

                MessageBox.Show("Start speaking your findings. The text will appear in real-time.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while starting findings recognition: {ex.Message}");
            }
        }

        private void StopListening()
        {
            try
            {
                // Stop recognizing findings and return to command listening mode
                speechRecognitionEngine.SpeechRecognized -= FindingsSpeechRecognized;
                speechRecognitionEngine.SpeechRecognized += ControlCommandsSpeechRecognized;

                // Stop ongoing recognition session
                speechRecognitionEngine.RecognizeAsyncStop();

                MessageBox.Show("Stopped listening.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping speech recognition: {ex.Message}");
            }
        }

        private void FindingsSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            try
            {
                txtFindings.AppendText(e.Result.Text + " ");
                txtFindings.ScrollToEnd();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing speech for findings: {ex.Message}");
            }
        }



        private void SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            // Handle unrecognized speech (optional, you can adjust this as needed)
            MessageBox.Show("Sorry, I didn't understand that. Please try again.");
        }

        private void SpeechAudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            // Optional: Handle audio level updates (for debugging or feedback on audio input quality)
            Console.WriteLine($"Audio level: {e.AudioLevel}");
        }

        private async void btnProvideFeedback_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string feedbackText = txtFeedback.Text.Trim();
                TextRange textRange = new TextRange(txtReport.Document.ContentStart, txtReport.Document.ContentEnd);
               // string currentReport = txtReport.text.Trim();
                string currentReport = textRange.Text.Trim();

                if (string.IsNullOrEmpty(feedbackText))
                {
                    MessageBox.Show("Please enter feedback before submitting.");
                    return;
                }

                string feedbackRequest = $"Modify the following ultrasound report based on this feedback:\n\nReport: {currentReport}\n\nFeedback: {feedbackText}";
                string updatedReport = await GetChatGptResponseAsync(feedbackRequest);

                if (!string.IsNullOrEmpty(updatedReport))
                {
                    textRange.Text = updatedReport;
                    txtFeedback.Clear();
                }
                else
                {
                    MessageBox.Show("No response received from the API.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing feedback: {ex.Message}");
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreWindow_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

       

      

        private async void btnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string findingsText = txtFindings.Text;

                if (string.IsNullOrEmpty(findingsText))
                {
                    MessageBox.Show("Please enter findings before generating a report.");
                    return;
                }

                // Fetch the report from GPT model
                string response = await GetChatGptResponseAsync(findingsText);

                //if (!string.IsNullOrEmpty(response))
                //{
                //    // Clean and format the response for better readability
                //    response = CleanUpReport(response);

                //    // Display the cleaned report in the report TextBox
                //    TextRange textRange = new TextRange(txtReport.Document.ContentStart, txtReport.Document.ContentEnd);
                //    textRange.Text = response;
                //}
                //else
                //{
                //    MessageBox.Show("No response received from the API.");
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}");
            }
        }

        // Helper method to clean up unwanted characters and improve formatting
        // Helper method to clean up unwanted characters and improve formatting
        // Helper method to clean up unwanted characters and improve formatting
        private string CleanUpReport(string input)
        {
            // Step 1: Remove unwanted characters such as asterisks (*), carriage returns (\r), and tabs (\t)
            string cleanedText = input.Replace("*", string.Empty)
                                       .Replace("\r", string.Empty)  // Remove carriage returns
                                       .Replace("\t", string.Empty); // Remove tabs

            // Step 2: Replace multiple newlines with a single newline for consistent paragraph spacing
            cleanedText = System.Text.RegularExpressions.Regex.Replace(cleanedText, @"(\n)+", "\n");

            // Step 3: Replace multiple spaces with a single space to clean up excessive gaps
            cleanedText = System.Text.RegularExpressions.Regex.Replace(cleanedText, @"\s+", " ");

            // Step 4: Trim leading and trailing whitespace
            cleanedText = cleanedText.Trim();

            // Step 5: Standardize section headers for clarity (no markdown symbols, just text)
            cleanedText = cleanedText.Replace("Findings:", "\nFindings:\n")
                                      .Replace("Conclusion:", "\nConclusion:\n")
                                      .Replace("Recommendations:", "\nRecommendations:\n");

            // Step 6: Standardize bullet points or list items with clear line breaks
            cleanedText = cleanedText.Replace("- ", "\n- ");  // Ensure proper list item formatting

            // Step 7: Add extra line breaks for readability between major sections
            cleanedText = cleanedText.Replace("\n\n", "\n\n"); // Ensure space between sections for clarity

            // Step 8: Ensure proper spacing between paragraphs and sections
            cleanedText = cleanedText.Replace("\n\n", "\n\n");

            // Return cleaned and formatted text
            return cleanedText;
        }





        private void btnPrintReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextRange textRange = new TextRange(txtReport.Document.ContentStart, txtReport.Document.ContentEnd);
                string reportContent = textRange.Text.Trim();

                if (string.IsNullOrEmpty(reportContent))
                {
                    MessageBox.Show("No report to print.");
                    return;
                }

                string finalizedReport = ApplyLetterheadTemplate(reportContent);
                PrintReport(finalizedReport);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during print: {ex.Message}");
            }
        }

        private void btnSaveReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // string reportContent = txtReport.Text.Trim();

                TextRange textRange = new TextRange(txtReport.Document.ContentStart, txtReport.Document.ContentEnd);
                string reportContent = ApplyLetterheadTemplate(textRange.Text);
                if (string.IsNullOrWhiteSpace(reportContent))
                {
                    MessageBox.Show("There is no report to save.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text File (*.txt)|*.txt|Word Document (*.docx)|*.docx|PDF File (*.pdf)|*.pdf|All Files (*.*)|*.*",
                    Title = "Save Report",
                    FileName = "USG_Report"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    string extension = Path.GetExtension(filePath).ToLower();

                    // Enforce correct file extension if the user removes it manually
                    if (string.IsNullOrEmpty(extension))
                    {
                        filePath += ".txt"; // Default to .txt if no extension is provided
                        extension = ".txt";
                    }

                    if (extension == ".txt")
                    {
                        SaveAsTextFile(filePath, reportContent);
                    }
                    else if (extension == ".docx")
                    {
                        SaveAsWordDocument(filePath, reportContent);
                    }
                    else if (extension == ".pdf")
                    {
                        SaveAsPdf(filePath, reportContent);
                    }
                    else
                    {
                        MessageBox.Show("Unsupported file format. Please choose .txt, .docx, or .pdf.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    MessageBox.Show($"Report saved successfully!\nLocation: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Permission denied. Try saving to a different location.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"File error: {ioEx.Message}", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ Saves report as a UTF-8 encoded text file safely
        private void SaveAsTextFile(string filePath, string content)
        {
            try
            {
                File.WriteAllText(filePath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save text file: {ex.Message}");
            }
        }

        // ✅ Saves report as a properly formatted Word (.docx) document
        private void SaveAsWordDocument(string filePath, string content)
        {
            try
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                {
                    // Add the main document part
                    MainDocumentPart mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();

                    // Create a body
                    Body body = new Body();

                    // Split content into lines and add each as a paragraph
                    string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    foreach (string line in lines)
                    {
                        // Use the Open XML Paragraph class
                        DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                            new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text(line))
                        );

                        // Add spacing between paragraphs (optional)
                        ParagraphProperties paraProps = new ParagraphProperties(
                            new SpacingBetweenLines { After = "200" }  // Adds space after each paragraph
                        );
                        paragraph.PrependChild(paraProps);

                        body.Append(paragraph);
                    }

                    // Append body to document
                    mainPart.Document.Append(body);
                    mainPart.Document.Save();
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save Word document: {ex.Message}");
            }
        }

        // Email Report Button Click
        private void btnEmailReport_Click(object sender, RoutedEventArgs e)
        {
            string recipientEmail = txtRecipientEmail.Text;
            string reportContent = new TextRange(txtReport.Document.ContentStart, txtReport.Document.ContentEnd).Text;

            if (string.IsNullOrEmpty(recipientEmail) || string.IsNullOrEmpty(reportContent))
            {
                MessageBox.Show("Please provide a recipient email and generate the report first.", "Email Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                // Validate email format
                if (!IsValidEmail(recipientEmail))
                {
                    MessageBox.Show("Please enter a valid recipient email address.", "Invalid Email", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // Create email and send it (using SMTP)
                    MailMessage mail = new MailMessage("deepkc256@gmail.com", recipientEmail)
                    {
                        Subject = "USG Report",
                        Body = reportContent,
                        IsBodyHtml = false // Set to true if you need HTML content in the email body
                    };

                    // Create SMTP client and configure
                    SmtpClient smtpClient = new SmtpClient("smtp.gmail.com") // Replace with your SMTP server
                    {
                        Port = 587, // For Gmail (use 465 for SSL or 25 for non-secure)
                        Credentials = new System.Net.NetworkCredential("deepkc256@gmail.com", "hsrptisakxnxkimj"), // Use app-specific passwords for better security
                        EnableSsl = true
                    };

                    // Send the email
                    smtpClient.Send(mail);
                    MessageBox.Show("Report emailed successfully!", "Email Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (SmtpException smtpEx)
                {
                    MessageBox.Show($"SMTP error: {smtpEx.Message}", "Email Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to send email: {ex.Message}", "Email Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Method to validate email format
        private bool IsValidEmail(string email)
        {
            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(email);
                return mailAddress.Address == email; // Ensures the email format is valid
            }
            catch
            {
                return false;
            }
        }





        // ✅ Saves report as a PDF file with proper formatting using iTextSharp
        private void SaveAsPdf(string filePath, string content)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    // Create a document and a writer for the PDF
                    Document pdfDoc = new Document(PageSize.A4, 50, 50, 50, 50);
                    PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                    pdfDoc.Open();

                    // Set font (using iTextSharp 3.7.1)
                    BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    Font titleFont = new Font(baseFont, 18, Font.BOLD);
                    Font contentFont = new Font(baseFont, 12, Font.NORMAL);

                    // Title
                    Paragraph title = new Paragraph("USG Report\n\n", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER
                    };
                    pdfDoc.Add(title);

                    // Report Content
                    Paragraph paragraph = new Paragraph(content, contentFont);
                    pdfDoc.Add(paragraph);

                    pdfDoc.Close();
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save PDF file: {ex.Message}");
            }
        }





        private string ApplyLetterheadTemplate(string reportContent)
        {
            return $@"
===================================================
                 HOSPITAL LETTERHEAD
===================================================
Patient Name: [Patient Name]  
Age / Gender: [Age] / [Gender]  
Referring Physician: [Doctor's Name]  
Examination Type: [USG Type]  

---------------------------------------------------
                      FINDINGS
---------------------------------------------------
{reportContent}

---------------------------------------------------
                     IMPRESSION
---------------------------------------------------
===================================================
Radiologist's Signature
(Doctor's Name, Qualification, Registration Number)
===================================================
";
        }

        //public void PrintReport(string reportContent)
        //{
        //    try
        //    {
        //        PrintDialog printDialog = new PrintDialog();

        //        if (printDialog.ShowDialog() == true)
        //        {
        //            System.Windows.Documents.FlowDocument flowDocument = new System.Windows.Documents.FlowDocument(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(reportContent)))
        //            {
        //                FontSize = 12,
        //                FontFamily = new System.Windows.Media.FontFamily("Arial")
        //            };

        //            printDialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)flowDocument).DocumentPaginator, "USG Report");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error printing report: {ex.Message}");
        //    }
        //}


        public void PrintReport(string reportContent)
        {
            try
            {
                // Create the formatted report document using SetFormattedReportText
                SetFormattedReportText(reportContent);

                // Show the PrintDialog to the user
                PrintDialog printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    // Add the letterhead image at the top
                    System.Windows.Controls.Image letterheadImage = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri("C:\\Users\\kiran\\Desktop\\lettter.png")),
                        Width = 600,
                        Height = 100
                    };

                    // Insert the letterhead image at the top of the document
                    FlowDocument flowDocument = txtReport.Document as FlowDocument;
                    if (flowDocument != null)
                    {
                        flowDocument.Blocks.InsertBefore(flowDocument.Blocks.FirstBlock, new BlockUIContainer(letterheadImage));

                        // Print the formatted document
                        printDialog.PrintDocument(((IDocumentPaginatorSource)flowDocument).DocumentPaginator, "USG Report");
                    }
                    else
                    {
                        MessageBox.Show("Error: Could not generate the formatted document.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing report: {ex.Message}");
            }
        }







        private static readonly HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };

        //private async Task<string> GetChatGptResponseAsync(string inputText)
        //{
        //    try
        //    {
        //        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        //        var requestData = new
        //        {
        //            model = "gpt-4o-mini",
        //            messages = new[]
        //            {
        //        new { role = "user", content = inputText }
        //    }
        //        };

        //        var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

        //        HttpResponseMessage response = await httpClient.PostAsync("v1/chat/completions", content);

        //        if (!response.IsSuccessStatusCode)
        //            return $"Error: {response.StatusCode} - {response.ReasonPhrase}";

        //        var jsonResponse = await response.Content.ReadAsStringAsync();
        //        var openAiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(jsonResponse);

        //        return openAiResponse?.choices?.FirstOrDefault()?.message?.content ?? "No response from AI.";
        //    }
        //    catch (HttpRequestException httpEx)
        //    {
        //        return $"HTTP Request Exception: {httpEx.Message}";
        //    }
        //    catch (Exception ex)
        //    {
        //        return $"Exception: {ex.Message}";
        //    }

        private async Task<string> GetChatGptResponseAsync(string inputText)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var requestData = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                new
                {
                    role = "user",
                    content = $@"
You are an AI specialized in creating structured and professional ultrasound (USG) diagnostic reports.

Using the clinical findings provided below, generate a detailed, accurate, and formally written ultrasound diagnostic report. Ensure proper formatting and medical terminology.

Clinical Findings:  
{inputText}

The report must follow this exact format:

Ultrasound Diagnostic Report

Patient Information  
Patient Name: [Patient Name]  
Date of Examination: [Date]  
Referring Physician: [Physician Name]  
Clinical Indication: [Clinical Indication]

Findings :  
Describe anatomical structures in order (Uterus, Ovaries, Adnexa, Liver, Gallbladder, Kidneys, etc.). Use concise, clinical language with measurements where applicable.

Abnormal Findings : 
List abnormal observations with location, characteristics, and dimensions. Use clear indentation and spacing for readability.

Free Fluid :
State whether free fluid is present or absent in the pelvis or abdomen.

Impression :  
Summarize all significant findings using formal radiology language.

Recommendations :  
Include relevant follow-up suggestions or additional investigations.

Reporting Radiologist  
Radiologist Name: [Radiologist Name]  
Signature: ___________________  
Date of Report: [Report Date]

Formatting Guidelines:  
- Maintain clean spacing and professional tone  
- Avoid asterisks, bullets, or informal formatting  
- Output only the complete, polished report
- Reduce space."
                }
            }
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync("v1/chat/completions", content);

                // Handle Rate Limit (429)
                if ((int)response.StatusCode == 429)
                {
                    if (response.Headers.TryGetValues("retry-after-ms", out var retryAfterMsValues) &&
                        int.TryParse(retryAfterMsValues.FirstOrDefault(), out int retryMs))
                    {
                        await Task.Delay(retryMs);
                        return await GetChatGptResponseAsync(inputText); // Retry after waiting
                    }

                    return "Error: Too many requests. Please try again later.";
                }

                if (!response.IsSuccessStatusCode)
                    return $"Error: {response.StatusCode} - {response.ReasonPhrase}";

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var openAiResponse = JsonConvert.DeserializeObject<OpenAIResponse>(jsonResponse);

                string responseContent = openAiResponse?.choices?.FirstOrDefault()?.message?.content ?? "No response from AI.";

                // Display formatted text
                SetFormattedReportText(responseContent);

                return responseContent;
            }
            catch (HttpRequestException httpEx)
            {
                return $"HTTP Request Exception: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }



        // Convert the response content into rich text and display it in the RichTextBox
        private void SetFormattedReportText(string content)
        {
            var flowDocument = new System.Windows.Documents.FlowDocument
            {
                LineHeight = 1.0, // Reduce line height
                PagePadding = new Thickness(0),
                Background = Brushes.Transparent

            };

            string[] lines = content.Split(new[] { "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var paragraph = new System.Windows.Documents.Paragraph
                {
                    Margin = new Thickness(0), // Remove margin
                    TextAlignment = System.Windows.TextAlignment.Left,
                    LineHeight = 1.0, // Set line height to 1.0 to minimize vertical space
                };

                bool isHeading = line.EndsWith(":") ||
                          line.Contains("Findings") ||
                          line.Contains("Impression") ||
                          line.Contains("Recommendations") ||
                          line.Contains("Abnormal Findings") ||
                          line.Contains("Patient Information") ||
                          line.Contains("Referring Physician") ||
                          line.Contains("Reporting Radiologist");


                var run = new System.Windows.Documents.Run(line);

                if (isHeading)
                {
                    run.FontWeight = System.Windows.FontWeights.Bold;
                    run.FontSize = 16; // Reduced font size for headings
                    run.Foreground = Brushes.Black;
                }
                else
                {
                    run.FontSize = 14; // Reduced font size for regular text
                    run.Foreground = Brushes.Black;
                }

                paragraph.Inlines.Add(run);
                flowDocument.Blocks.Add(paragraph);
            }

            txtReport.Document = flowDocument;
          
        }




        //private void btnPreviewReport_Click(object sender, RoutedEventArgs e)
        //{
        //    string reportContent = ApplyLetterheadTemplate(txtReport.Text);
        //    MessageBox.Show($"Preview:\n\n{reportContent}");
        //}

        private void btnPreviewReport_Click(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new TextRange(txtReport.Document.ContentStart, txtReport.Document.ContentEnd);
            string reportContent = textRange.Text;

            string letterheadPath = "C:\\Users\\kiran\\Desktop\\lettter.png"; // Your image path

            PrintPreviewWindow previewWindow = new PrintPreviewWindow(reportContent, letterheadPath);
            previewWindow.ShowDialog();
        }









        public class OpenAIResponse
        {
            public Choice[] choices { get; set; }
        }

        public class Choice
        {
            public Message message { get; set; }
        }

        public class Message
        {
            public string content { get; set; }
        }


    }
}
