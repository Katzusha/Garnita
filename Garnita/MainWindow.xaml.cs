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
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Garnita
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string connstring = "Server=ep-purple-mode-252889.eu-central-1.aws.neon.tech;Database=Garnita;User Id=andraz.kosak;Password=2tUHlDPGdn7x;";
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

        public int ModifiedRent = 0;
        public int ModifiedGarage = 0;
        public int ModifiedCar = 0;


        public MainWindow()
        {
            InitializeComponent();

            LoginScreen.Visibility = Visibility.Visible;
            RegisterScreen.Visibility = Visibility.Hidden;
            RentScreen.Visibility = Visibility.Hidden;
            CreateRentScreen.Visibility = Visibility.Hidden;
            GarageScreen.Visibility = Visibility.Hidden;
            CreateGarageScreen.Visibility = Visibility.Hidden;
            CarScreen.Visibility = Visibility.Hidden;
            CreateCarScreen.Visibility = Visibility.Hidden;

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

            ChangeColor();
        }

        public void ChangeColor()
        {
            try
            {

                conn.Open();

                string strquery = "SELECT * FROM design";

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {

                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        Resources["PrimaryColor"] = (SolidColorBrush)new BrushConverter().ConvertFrom("#" + reader.GetString(1));
                        Resources["SecondaryColor"] = (Color)ColorConverter.ConvertFromString("#" + reader.GetString(2));
                        this.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#" + reader.GetString(3));
                        Resources["FontColor"] = (SolidColorBrush)new BrushConverter().ConvertFrom("#" + reader.GetString(4));
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
            CreateRentScreen.Visibility = Visibility.Hidden;

            try
            {
                RentsGrid.RowDefinitions.Clear();
                RentsGrid.Children.Clear();

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
                        btn.Name = "RentId" + reader.GetInt32(5).ToString();
                        btn.Content = (reader.GetString(0).ToString()) + " " + (reader.GetString(1).ToString()) +
                            "\t\t\t" + reader.GetString(2).ToString().Replace('-', '.') + " - " + reader.GetString(3).ToString().Replace('-', '.') +
                            "\t\t\t" + reader.GetString(4).ToString();
                        btn.Style = (Style)this.Resources["GeneratedButton"];
                        btn.Click += new RoutedEventHandler(EditRent);


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

        private void EditRent(object sender, RoutedEventArgs e)
        {
            RentScreen.Visibility = Visibility.Hidden;
            CreateRentScreen.Visibility = Visibility.Visible;

            Button btn = (Button)sender;

            ModifiedRent = Int32.Parse(btn.Name.ToString().Replace("RentId", ""));

            CreateRentButton.Content = "Save";

            try
            {
                conn.Open();

                string strquery = "SELECT email FROM users";

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = reader.GetString(0).ToString();
                        RentUserInput.Items.Add(item);
                    }
                }

                strquery = "SELECT licenceplate FROM cars";

                cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = reader.GetString(0).ToString();
                        RentCarInput.Items.Add(item);
                    }
                }

                strquery = "SELECT * FROM getrentinfo(" + btn.Name.ToString().Replace("RentId", "") + ")";

                cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        RentUserInput.Text = reader.GetString(0).ToString();
                        RentCarInput.Text = reader.GetString(1).ToString();
                        RentFromDateInput.Text = reader.GetDateTime(2).ToString();
                        RentToDateInput.Text = reader.GetDateTime(3).ToString();
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

        private void CreateRentButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModifiedRent == 0)
            {
                try
                {
                    conn.Open();

                    string strquery = ("SELECT createrent('" + RentUserInput.Text + "', '" + RentCarInput.Text + "', '" +
                        RentFromDateInput.SelectedDate.Value.ToString("yyyy-MM-dd") + "', '" +
                        RentToDateInput.SelectedDate.Value.ToString("yyyy-MM-dd") + "')");

                    NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                    if ((Int32)cmd.ExecuteScalar() == 1)
                    {
                        RentUserInput.SelectedIndex = -1;
                        RentUserInput.Items.Clear();
                        RentCarInput.SelectedIndex = -1;
                        RentCarInput.Items.Clear();

                        RentFromDateInput.SelectedDate = DateTime.Now;
                        RentToDateInput.SelectedDate = DateTime.Now;

                        conn.Close();

                        GenerateRents();
                    }
                    else if ((Int32)cmd.ExecuteScalar() == 0)
                    {
                        RentUserInput.SelectedIndex = -1;
                        RentUserInput.Items.Clear();
                        RentCarInput.SelectedIndex = -1;
                        RentCarInput.Items.Clear();

                        RentFromDateInput.SelectedDate = DateTime.Now;
                        RentToDateInput.SelectedDate = DateTime.Now;

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
            else if (ModifiedRent != 0)
            {
                try
                {
                    conn.Open();

                    string strquery = ("SELECT editrent('" + RentUserInput.Text + "', '" + RentCarInput.Text + "', '" +
                        RentFromDateInput.SelectedDate.Value.ToString("yyyy-MM-dd") + "', '" +
                        RentToDateInput.SelectedDate.Value.ToString("yyyy-MM-dd") + "', '" + ModifiedRent.ToString() + "')");

                    NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                    if ((Int32)cmd.ExecuteScalar() == 1)
                    {
                        RentUserInput.SelectedIndex = -1;
                        RentUserInput.Items.Clear();
                        RentCarInput.SelectedIndex = -1;
                        RentCarInput.Items.Clear();

                        RentFromDateInput.SelectedDate = DateTime.Now;
                        RentToDateInput.SelectedDate = DateTime.Now;

                        conn.Close();

                        GenerateRents();
                    }
                    else if ((Int32)cmd.ExecuteScalar() == 0)
                    {
                        RentUserInput.SelectedIndex = -1;
                        RentUserInput.Items.Clear();
                        RentCarInput.SelectedIndex = -1;
                        RentCarInput.Items.Clear();

                        RentFromDateInput.SelectedDate = DateTime.Now;
                        RentToDateInput.SelectedDate = DateTime.Now;

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

        private void CancelRentButton_Click(object sender, RoutedEventArgs e)
        {
            RentUserInput.SelectedIndex = -1;
            RentUserInput.Items.Clear();
            RentCarInput.SelectedIndex = -1;
            RentCarInput.Items.Clear();

            RentFromDateInput.SelectedDate = DateTime.Now;
            RentToDateInput.SelectedDate = DateTime.Now;

            GenerateRents();

            CreateRentButton.Content = "Create";
            ModifiedRent = 0;
        }

        private void AddRentButton_Click(object sender, RoutedEventArgs e)
        {
            RentScreen.Visibility = Visibility.Hidden;
            CreateRentScreen.Visibility = Visibility.Visible;

            try
            {
                conn.Open();

                string strquery = "SELECT email FROM users";

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = reader.GetString(0).ToString();
                        RentUserInput.Items.Add(item);
                    }
                }

                strquery = "SELECT licenceplate FROM cars";

                cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = reader.GetString(0).ToString();
                        RentCarInput.Items.Add(item);
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

        public void GenerateGarages()
        {
            try
            {
                conn.Open();

                string strquery = "select * from getgarages";

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    int x = 0;

                    GaragesGrid.RowDefinitions.Clear();
                    GaragesGrid.Children.Clear();

                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        RowDefinition newrow = new RowDefinition();
                        newrow.Height = new GridLength(75);
                        GaragesGrid.RowDefinitions.Add(newrow);

                        Button btn = new Button();
                        btn.Name = "GarageId" + reader.GetInt32(0).ToString();
                        btn.Content = reader.GetString(1).ToString() + "\n" + reader.GetString(2).ToString() +
                            " " + reader.GetInt32(3).ToString();
                        btn.Style = (Style)this.Resources["GeneratedButton"];
                        btn.Click += new RoutedEventHandler(EditGarage);


                        Grid.SetRow(btn, x);
                        GaragesGrid.Children.Add(btn);

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

        private void EditGarage(object sender, RoutedEventArgs e)
        {
            GarageScreen.Visibility = Visibility.Hidden;
            CreateGarageScreen.Visibility = Visibility.Visible;

            Button btn = (Button)sender;

            ModifiedGarage = Int32.Parse(btn.Name.ToString().Replace("GarageId", ""));

            CreateGarageButton.Content = "Save";

            try
            {
                conn.Open();

                string strquery = "SELECT * FROM citys";

                GarageCityInput.Items.Clear();

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Name = "CityId" + reader.GetInt32(0).ToString();
                        item.Content = reader.GetString(1).ToString() + ", " + reader.GetInt32(2).ToString();
                        GarageCityInput.Items.Add(item);
                    }
                }

                strquery = "SELECT * FROM getgarageinfo('" + ModifiedGarage.ToString() + "')";

                cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        GarageNameINput.Text = reader.GetString(0).ToString();
                        GarageCityInput.Text = reader.GetString(1).ToString() + ", " + reader.GetInt32(2).ToString();
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

        private void GarageButton_Click(object sender, RoutedEventArgs e)
        {
            LoginScreen.Visibility = Visibility.Hidden;
            RentScreen.Visibility = Visibility.Hidden;
            GarageScreen.Visibility = Visibility.Visible;
            CreateRentScreen.Visibility = Visibility.Hidden;

            GenerateGarages();
        }

        private void GarageBackButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateRents();
        }

        private void AddGarageButton_Click(object sender, RoutedEventArgs e)
        {
            GarageScreen.Visibility = Visibility.Hidden;
            CreateGarageScreen.Visibility = Visibility.Visible;

            try
            {
                conn.Open();

                string strquery = "SELECT * FROM citys";

                GarageCityInput.Items.Clear();

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Name = "CityId" + reader.GetInt32(0).ToString();
                        item.Content = reader.GetString(1).ToString() + ", " + reader.GetInt32(2).ToString();
                        GarageCityInput.Items.Add(item);
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

        private void CreateGarageButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModifiedGarage == 0)
            {
                try
                {
                    ComboBoxItem item = (ComboBoxItem)GarageCityInput.SelectedItem;

                    conn.Open();

                    string strquery = ("SELECT creategarage ('" + GarageNameINput.Text + "', '" + item.Name.ToString().Replace("CityId", "") + "')");
                    NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                    if ((Int32)cmd.ExecuteScalar() == 1)
                    {
                        RentUserInput.SelectedIndex = -1;
                        RentUserInput.Items.Clear();
                        RentCarInput.SelectedIndex = -1;
                        RentCarInput.Items.Clear();

                        RentFromDateInput.SelectedDate = DateTime.Now;
                        RentToDateInput.SelectedDate = DateTime.Now;

                        CreateGarageScreen.Visibility = Visibility.Hidden;
                        GarageScreen.Visibility = Visibility.Visible;
                    }
                    else if ((Int32)cmd.ExecuteScalar() == 0)
                    {
                        RentUserInput.SelectedIndex = -1;
                        RentUserInput.Items.Clear();
                        RentCarInput.SelectedIndex = -1;
                        RentCarInput.Items.Clear();

                        RentFromDateInput.SelectedDate = DateTime.Now;
                        RentToDateInput.SelectedDate = DateTime.Now;

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
            else if (ModifiedGarage != 0)
            {
                try
                {
                    conn.Open();

                    ComboBoxItem item = (ComboBoxItem)GarageCityInput.SelectedItem;

                    string strquery = ("SELECT editgarage ('" + GarageNameINput.Text + "', '" + item.Name.ToString().Replace("CityId", "") +
                        "', '" + ModifiedGarage + "')");
                    NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                    if ((Int32)cmd.ExecuteScalar() == 1)
                    {
                        conn.Close();

                        GarageScreen.Visibility = Visibility.Visible;
                        CreateGarageScreen.Visibility = Visibility.Hidden;

                        ModifiedGarage = 0;

                        GenerateGarages();
                    }
                    else if ((Int32)cmd.ExecuteScalar() == 0)
                    {
                        GarageNameINput.Clear();
                        GarageCityInput.SelectedIndex = -1;

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

        private void CancelGarageButton_Click(object sender, RoutedEventArgs e)
        {
            GarageScreen.Visibility = Visibility.Visible;
            CreateGarageScreen.Visibility = Visibility.Hidden;

            ModifiedGarage = 0;

            GenerateGarages();
        }

        public void GenerateCars()
        {
            try
            {
                conn.Open();

                string strquery = "select * from getcars";

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    int x = 0;

                    CarsGrid.RowDefinitions.Clear();
                    CarsGrid.Children.Clear();

                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        RowDefinition newrow = new RowDefinition();
                        newrow.Height = new GridLength(75);
                        CarsGrid.RowDefinitions.Add(newrow);

                        Button btn = new Button();
                        btn.Name = "CarId" + reader.GetInt32(0).ToString();
                        btn.Content = reader.GetString(1).ToString() + " - " + reader.GetString(2).ToString() +
                            "\n" + reader.GetString(3).ToString();
                        btn.Style = (Style)this.Resources["GeneratedButton"];
                        btn.Click += new RoutedEventHandler(EditCar);


                        Grid.SetRow(btn, x);
                        CarsGrid.Children.Add(btn);

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

        public void EditCar(object sender, RoutedEventArgs e)
        {
            CarScreen.Visibility = Visibility.Hidden;
            CreateCarScreen.Visibility = Visibility.Visible;

            Button btn = (Button)sender;

            ModifiedCar = Int32.Parse(btn.Name.ToString().Replace("CarId", ""));

            CreateCarButton.Content = "Save";

            try
            {
                conn.Open();

                string strquery = "select * from getgarages";

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    CarGarageInput.Items.Clear();

                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Name = "GarageId" + reader.GetInt32(0).ToString();
                        item.Content = reader.GetString(1).ToString() + ", " + reader.GetString(2).ToString();
                        CarGarageInput.Items.Add(item);
                    }
                }


                strquery = "SELECT * FROM getcarinfo('" + ModifiedCar.ToString() + "')";

                cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        CarNameInput.Text = reader.GetString(0).ToString();
                        CarLicenceplateInput.Text = reader.GetString(1).ToString();
                        CarGarageInput.Text = reader.GetString(2).ToString();
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

        private void CarsButton_Click(object sender, RoutedEventArgs e)
        {
            RentScreen.Visibility = Visibility.Hidden;
            CarScreen.Visibility = Visibility.Visible;

            GenerateCars();
        }

        private void AddCarButton_Click(object sender, RoutedEventArgs e)
        {
            CarScreen.Visibility = Visibility.Hidden;
            CreateCarScreen.Visibility = Visibility.Visible;

            try
            {
                conn.Open();

                string strquery = "select * from getgarages";

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    CarGarageInput.Items.Clear();

                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Name = "GarageId" + reader.GetInt32(0).ToString();
                        item.Content = reader.GetString(1).ToString() + ", " + reader.GetString(2).ToString();
                        CarGarageInput.Items.Add(item);
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

        private void CreateCarButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModifiedCar == 0)
            {
                try
                {
                    ComboBoxItem item = (ComboBoxItem)CarGarageInput.SelectedItem;

                    conn.Open();

                    string strquery = ("SELECT createcar ('" + CarNameInput.Text + "', '" + CarLicenceplateInput.Text + "', '" + item.Name.ToString().Replace("GarageId", "") + "')");
                    NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                    if ((Int32)cmd.ExecuteScalar() == 1)
                    {
                        CarGarageInput.SelectedIndex = -1;
                        CarGarageInput.Items.Clear();
                        CarNameInput.Clear();
                        CarLicenceplateInput.Clear();

                        CreateCarScreen.Visibility = Visibility.Hidden;
                        CarScreen.Visibility = Visibility.Visible;

                        conn.Close();

                        GenerateCars();
                    }
                    else if ((Int32)cmd.ExecuteScalar() == 0)
                    {
                        CarGarageInput.SelectedIndex = -1;
                        CarGarageInput.Items.Clear();
                        CarNameInput.Clear();
                        CarLicenceplateInput.Clear();

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
            else if (ModifiedCar != 0)
            {
                try
                {
                    conn.Open();

                    ComboBoxItem item = (ComboBoxItem)CarGarageInput.SelectedItem;

                    string strquery = ("SELECT editcar ('" + ModifiedCar.ToString() + "', '" + CarNameInput.Text + "', '" + CarLicenceplateInput.Text + "', '" + item.Name.ToString().Replace("GarageId", "") + "')");
                    NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                    if ((Int32)cmd.ExecuteScalar() == 1)
                    {
                        CarGarageInput.SelectedIndex = -1;
                        CarGarageInput.Items.Clear();
                        CarNameInput.Clear();
                        CarLicenceplateInput.Clear();

                        CreateCarScreen.Visibility = Visibility.Hidden;
                        CarScreen.Visibility = Visibility.Visible;

                        conn.Close();

                        GenerateCars();
                    }
                    else if ((Int32)cmd.ExecuteScalar() == 0)
                    {
                        CarGarageInput.SelectedIndex = -1;
                        CarGarageInput.Items.Clear();
                        CarNameInput.Clear();
                        CarLicenceplateInput.Clear();

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

        private void CarBackButton_Click(object sender, RoutedEventArgs e)
        {
            CarScreen.Visibility = Visibility.Hidden;
            RentScreen.Visibility = Visibility.Visible;

            GenerateRents();
        }

        private void ChangeColorbutton_Click(object sender, RoutedEventArgs e)
        {
            RentScreen.Visibility = Visibility.Hidden;
            ColorScreen.Visibility = Visibility.Visible;

            try
            {
                conn.Open();

                string strquery = "select * from design";

                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    CarGarageInput.Items.Clear();

                    // Read the rows of the result set
                    while (reader.Read())
                    {
                        ColorPrimaryInput.Text = reader.GetString(1).ToString();
                        ColorSecondaryinput.Text = reader.GetString(2).ToString();
                        ColorBackgroundInput.Text = reader.GetString(3).ToString();
                        ColorFontinput.Text = reader.GetString(4).ToString();
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

        private void CancelColorButton_Click(object sender, RoutedEventArgs e)
        {
            RentScreen.Visibility = Visibility.Visible;
            ColorScreen.Visibility = Visibility.Hidden;
        }

        private void SaveColorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                conn.Open();

                ComboBoxItem item = (ComboBoxItem)CarGarageInput.SelectedItem;

                string strquery = ("SELECT editdesign('" + ColorPrimaryInput.Text + "', '" + ColorSecondaryinput.Text + "', '" + ColorBackgroundInput.Text + 
                    "', '" + ColorFontinput.Text + "')");
                NpgsqlCommand cmd = new NpgsqlCommand(strquery, conn);

                if ((Int32)cmd.ExecuteScalar() == 1)
                {
                    ColorScreen.Visibility = Visibility.Hidden;
                    RentScreen.Visibility = Visibility.Visible;

                    conn.Close();

                    ChangeColor();

                    GenerateRents();
                }
                else if ((Int32)cmd.ExecuteScalar() == 0)
                {
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
}
