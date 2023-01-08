using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Npgsql;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Garnita
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string connstring = "Server=ep-purple-mode-252889.eu-central-1.aws.neon.tech;Database=Garnita;User Id=andraz.kosak;Password=7xA0oyeXHNrQ;";
        NpgsqlConnection conn;

        #region ENCRYPTION AND DECRYPTION
        public string EncryptString(string plainText, byte[] key, byte[] iv)
        {
            // Instantiate a new Aes object to perform string symmetric encryption
            Aes encryptor = Aes.Create();

            encryptor.Mode = CipherMode.CBC;
            //encryptor.KeySize = 256;
            //encryptor.BlockSize = 128;
            //encryptor.Padding = PaddingMode.Zeros;

            // Set key and IV
            encryptor.Key = key;
            encryptor.IV = iv;

            // Instantiate a new MemoryStream object to contain the encrypted bytes
            MemoryStream memoryStream = new MemoryStream();

            // Instantiate a new encryptor from our Aes object
            ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();

            // Instantiate a new CryptoStream object to process the data and write it to the 
            // memory stream
            CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

            // Convert the plainText string into a byte array
            byte[] plainBytes = Encoding.ASCII.GetBytes(plainText);

            // Encrypt the input plaintext string
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);

            // Complete the encryption process
            cryptoStream.FlushFinalBlock();

            // Convert the encrypted data from a MemoryStream to a byte array
            byte[] cipherBytes = memoryStream.ToArray();

            // Close both the MemoryStream and the CryptoStream
            memoryStream.Close();
            cryptoStream.Close();

            // Convert the encrypted byte array to a base64 encoded string
            string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);

            // Return the encrypted data as a string
            return cipherText;
        }

        public string DecryptString(string cipherText, byte[] key, byte[] iv)
        {
            // Instantiate a new Aes object to perform string symmetric encryption
            Aes encryptor = Aes.Create();

            encryptor.Mode = CipherMode.CBC;
            //encryptor.KeySize = 256;
            //encryptor.BlockSize = 128;
            //encryptor.Padding = PaddingMode.Zeros;

            // Set key and IV
            encryptor.Key = key;
            encryptor.IV = iv;

            // Instantiate a new MemoryStream object to contain the encrypted bytes
            MemoryStream memoryStream = new MemoryStream();

            // Instantiate a new encryptor from our Aes object
            ICryptoTransform aesDecryptor = encryptor.CreateDecryptor();

            // Instantiate a new CryptoStream object to process the data and write it to the 
            // memory stream
            CryptoStream cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);

            // Will contain decrypted plaintext
            string plainText = String.Empty;

            try
            {
                // Convert the ciphertext string into a byte array
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                // Decrypt the input ciphertext string
                cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);

                // Complete the decryption process
                cryptoStream.FlushFinalBlock();

                // Convert the decrypted data from a MemoryStream to a byte array
                byte[] plainBytes = memoryStream.ToArray();

                // Convert the decrypted byte array to string
                plainText = Encoding.ASCII.GetString(plainBytes, 0, plainBytes.Length);
            }
            finally
            {
                // Close both the MemoryStream and the CryptoStream
                memoryStream.Close();
                cryptoStream.Close();
            }

            // Return the decrypted data as a string
            return plainText;
        }

        public string Encryption(string encrypt)
        {
            string password = "3sc3RLrpd17";

            // Create sha256 hash
            SHA256 mySHA256 = SHA256Managed.Create();
            byte[] key = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(password));

            // Create secret IV
            byte[] iv = new byte[16] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };

            string encrypted = this.EncryptString(encrypt, key, iv);

            return encrypted;
        }

        public string Decryption(string decrypt)
        {
            string password = "3sc3RLrpd17";

            // Create sha256 hash
            SHA256 mySHA256 = SHA256Managed.Create();
            byte[] key = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(password));

            // Create secret IV
            byte[] iv = new byte[16] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };

            string decrypted = this.DecryptString(decrypt, key, iv);

            return decrypted;
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            LoginScreen.Visibility = Visibility.Visible;
            RegisterScreen.Visibility = Visibility.Hidden;
            RentScreen.Visibility = Visibility.Hidden;
            GarageScreen.Visibility = Visibility.Hidden;

            try
            {
                conn = new NpgsqlConnection(connstring);
                conn.Open();
                conn.Close();
            }
            catch(Exception ex)
            {
                conn.Close();
                MessageBox.Show(ex.Message);
            }
        }

        private void LogInButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                conn.Open();

                string strquery = ("SELECT login('" + Encryption(UsernameInput.Text) + "', '" +
                    Encryption(PasswordInput.Password.ToString()) + "')");

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                if ((Int32)cmd.ExecuteScalar() == 1)
                {
                    conn.Close();

                    GenerateRents();
                }
                else if ((Int32)cmd.ExecuteScalar() == 0)
                {
                    MessageBox.Show("Wrong username or password. Please try again!");
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                conn.Close();

                MessageBox.Show(ex.Message);
            }
        }

        private void CanselLoginButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RegisterButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoginScreen.Visibility = Visibility.Hidden;
            RegisterScreen.Visibility = Visibility.Visible;

            UsernameInput.Clear();
            PasswordInput.Clear();
        }

        private void GenerateRents()
        {
            LoginScreen.Visibility = Visibility.Hidden;
            RentScreen.Visibility = Visibility.Visible;
            GarageScreen.Visibility = Visibility.Hidden;

            try
            {
                conn.Open();

                string strquery = "SELECT * FROM getrents";

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    int x = 0;

                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        RowDefinition newrow = new RowDefinition();
                        newrow.Height = new GridLength(50);
                        RentsGrid.RowDefinitions.Add(newrow);

                        Button btn = new Button();
                        btn.Content = Decryption(reader.GetString(0).ToString()) + " " + Decryption(reader.GetString(1).ToString()) +
                            "\t\t\t" + reader.GetString(2).ToString().Replace('-', '.') + " - " + reader.GetString(3).ToString().Replace('-', '.') +
                            "\t\t\t" + reader.GetString(4).ToString();
                        btn.Style = (Style)this.Resources["GeneratedButton"];

                        Grid.SetRow(btn, x);
                        RentsGrid.Children.Add(btn);

                        x++;
                    }
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                conn.Close();

                MessageBox.Show(ex.Message);
            }
        }

        private void CanselCreateUserButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterScreen.Visibility = Visibility.Hidden;
            LoginScreen.Visibility = Visibility.Visible;

            RegisterFirstnameInput.Clear();
            RegisterSurnameInput.Clear();
            RegisterBirthInput.SelectedDate = DateTime.Now;
            RegisterEmailInput.Clear();
            RegisterUsernameInput.Clear();
            RegisterPasswordInput.Clear();
        }

        private void CreateUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                conn.Open();

                string strquery = ("SELECT createuser('" + RegisterFirstnameInput.Text + "', '" + RegisterSurnameInput.Text + "', '" + RegisterBirthInput.SelectedDate.Value.ToString("yyyy-MM-dd") + "', '" + RegisterEmailInput.Text + "', '" + Encryption(RegisterUsernameInput.Text) + "', '" + Encryption(RegisterPasswordInput.Text) + "')");

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                if ((Int32)cmd.ExecuteScalar() == 1)
                {
                    RegisterScreen.Visibility = Visibility.Hidden;
                    LoginScreen.Visibility = Visibility.Visible;

                    RegisterFirstnameInput.Clear();
                    RegisterSurnameInput.Clear();
                    RegisterBirthInput.SelectedDate = DateTime.Now;
                    RegisterEmailInput.Clear();
                    RegisterUsernameInput.Clear();
                    RegisterPasswordInput.Clear();
                }
                else if ((Int32)cmd.ExecuteScalar() == 0)
                {
                    RegisterFirstnameInput.Clear();
                    RegisterSurnameInput.Clear();
                    RegisterBirthInput.SelectedDate = DateTime.Now;
                    RegisterEmailInput.Clear();
                    RegisterUsernameInput.Clear();
                    RegisterPasswordInput.Clear();

                    MessageBox.Show("Wrong input. Please try again!");
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                conn.Close();

                MessageBox.Show(ex.Message);
            }
        }

        private void ForgotPassword_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LoginScreen.Visibility = Visibility.Hidden;
            ForgotenPasswordScreen.Visibility = Visibility.Visible;

            UsernameInput.Clear();
            PasswordInput.Clear();
        }

        private void ConfirmForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (ForgotPasswordInput.Text == ForgotPasswordInputTest.Password.ToString())
            {
                try
                {
                    conn.Open();

                    string strquery = ("SELECT changepassword('" + ForgotEmailInput.Text + "', '" +
                        Encryption(ForgotUsernameInput.Text) + "', '" + Encryption(ForgotPasswordInput.Text) + "')");

                    NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                    if ((Int32)cmd.ExecuteScalar() == 1)
                    {
                        LoginScreen.Visibility = Visibility.Visible;
                        ForgotenPasswordScreen.Visibility = Visibility.Hidden;

                        ForgotEmailInput.Clear();
                        ForgotUsernameInput.Clear();
                        ForgotPasswordInput.Clear();
                        ForgotPasswordInputTest.Clear();
                    }
                    else if ((Int32)cmd.ExecuteScalar() == 0)
                    {
                        ForgotEmailInput.Clear();
                        ForgotUsernameInput.Clear();
                        ForgotPasswordInput.Clear();
                        ForgotPasswordInputTest.Clear();

                        MessageBox.Show("Wrong input. Please try again!");
                    }

                    conn.Close();
                }
                catch (Exception ex)
                {
                    conn.Close();

                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void CancelForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            LoginScreen.Visibility = Visibility.Visible;
            ForgotenPasswordScreen.Visibility = Visibility.Hidden;

            ForgotEmailInput.Clear();
            ForgotUsernameInput.Clear();
            ForgotPasswordInput.Clear();
            ForgotPasswordInputTest.Clear();
        }
    }
}
