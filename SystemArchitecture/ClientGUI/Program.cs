using System;
using System.Windows.Forms;

namespace MLE_GUI
{
    /// Program: Entry point for the Machine Learning with Encryption (MLE) GUI application.
    /// 
    /// This is a Windows Forms application that provides an interactive interface for:
    /// - Encrypting sensitive data using homomorphic encryption
    /// - Performing machine learning inference on encrypted data
    /// - Decrypting and viewing prediction results
    /// 
    /// The application runs entirely locally - no server or network connection required.
    /// All encryption, ML processing, and decryption happens on the user's machine.
    static class Program
    {
        /// Main: Entry point for the application.
        /// 
        /// [STAThread] attribute is required for Windows Forms applications
        /// to ensure proper thread apartment model for COM components.
        [STAThread]
        static void Main()
        {
            // Enable modern Windows Forms visual styles
            // This gives the application a more polished, native Windows appearance
            Application.EnableVisualStyles();
            
            // Use compatible text rendering (GDI+ instead of GDI)
            // Provides better text rendering quality
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Launch the modern GUI interface
            // MainFormModern provides a step-by-step interactive experience
            // MainForm (commented out) is the older, simpler GUI version
            Application.Run(new MainFormModern());
        }
    }
}

