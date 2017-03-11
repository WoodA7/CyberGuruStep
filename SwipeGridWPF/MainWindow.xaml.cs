using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;

namespace SwipeGridWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Storyboard StBoard = new Storyboard();
        const bool ToLeft = false;
        const bool ToRight = true;

        private SqlConnection SqlConn()
        {
            return new SqlConnection(ConfigurationManager.ConnectionStrings["connstring"].ToString());
        }

        public MainWindow()
        {
            InitializeComponent();

            rbTrip.IsChecked = true;
            cbClass.SelectedItem = cbClass.Items[1];
            DepDate.SelectedDate = DateTime.Today;

            var connect = SqlConn();
            connect.Open();
            var command = connect.CreateCommand();
            command.CommandText = @"SELECT (Name) FROM [dbo].[City]";
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                cbFrom.Items.Add(reader[0]);
                cbTo.Items.Add(reader[0]);
            }
            cbFrom.SelectedItem = cbFrom.Items[0];

            connect.Close();

            //TcpListener listener = new TcpListener(IPAddress.Any, 80);
            //listener.Start();

            //while (true)
            //{
            //	var tcpStream = listener.AcceptTcpClient().GetStream();

            //	var response = @"<html>
            //	<HEAD>
            //	</HEAD>
            //	<BODY>Test</BODY>
            //	</html>";
            //	var buffer = Encoding.ASCII.GetBytes(response);

            //	tcpStream.Write(buffer, 0, buffer.Length);
            //	tcpStream.Flush();
            //	tcpStream.Close();
            //}
        }

        void ChangeGrid(FrameworkElement firstElement, FrameworkElement secondElement, bool changeSide)
        {
            StBoard?.Begin(this, true);

            var ticknessLeft = new Thickness(Width, 0, -Width, 0);
            var ticknessRight = new Thickness(-Width, 0, Width, 0);
            var ticknessClient = new Thickness(0, 0, 0, 0);

            var timeSpanStarting = TimeSpan.FromSeconds(0);
            var timeSpanStopping = TimeSpan.FromSeconds(1);

            var keyTimeStarting = KeyTime.FromTimeSpan(timeSpanStarting);
            var keyTimeStopping = KeyTime.FromTimeSpan(timeSpanStopping);

            secondElement.Margin = changeSide ? ticknessRight : ticknessLeft;
            secondElement.Visibility = Visibility.Visible;

            var storyboardTemp = new Storyboard();

            var currentThicknessAnimationUsingKeyFrames = new ThicknessAnimationUsingKeyFrames { BeginTime = timeSpanStarting };

            Storyboard.SetTargetName(currentThicknessAnimationUsingKeyFrames, firstElement.Name);
            Storyboard.SetTargetProperty(currentThicknessAnimationUsingKeyFrames, new PropertyPath("(FrameworkElement.Margin)"));
            currentThicknessAnimationUsingKeyFrames.KeyFrames.Add(new SplineThicknessKeyFrame(ticknessClient, keyTimeStarting));
            currentThicknessAnimationUsingKeyFrames.KeyFrames.Add(new SplineThicknessKeyFrame(changeSide ? ticknessLeft : ticknessRight, keyTimeStopping));

            storyboardTemp.Children.Add(currentThicknessAnimationUsingKeyFrames);

            var nextThicknessAnimationUsingKeyFrames = new ThicknessAnimationUsingKeyFrames { BeginTime = timeSpanStarting };

            Storyboard.SetTargetName(nextThicknessAnimationUsingKeyFrames, secondElement.Name);
            Storyboard.SetTargetProperty(nextThicknessAnimationUsingKeyFrames, new PropertyPath("(FrameworkElement.Margin)"));
            nextThicknessAnimationUsingKeyFrames.KeyFrames.Add(new SplineThicknessKeyFrame(changeSide ? ticknessRight : ticknessLeft, keyTimeStarting));
            nextThicknessAnimationUsingKeyFrames.KeyFrames.Add(new SplineThicknessKeyFrame(ticknessClient, keyTimeStopping));

            storyboardTemp.Children.Add(nextThicknessAnimationUsingKeyFrames);

            storyboardTemp.Completed += delegate
            {
                firstElement.Visibility = Visibility.Hidden;
                StBoard = null;
            };

            StBoard = storyboardTemp;
            BeginStoryboard(storyboardTemp);
        }

        static void message(string s)
        {
            MessageBox.Show(s);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (cbTo.SelectedItem == null)
            {
                message("Select your destination, please!");
                return;
            }

            if (DepDate.SelectedDate == null)
            {
                message("Select departure date, please!");
                return;
            }

            if (cbClass.SelectedItem == null)
            {
                message("Select prefferable class, please!");
                return;
            }

            if (rbRTrip.IsChecked == true && DepDate.SelectedDate == null)
            {
                message("Select return date, please!");
                return;
            }

            textBlock.Text = cbFrom.SelectedItem + " - " + cbTo.SelectedItem +
                            (rbRTrip.IsChecked == true ? " - " + cbFrom.SelectedItem : "") + Environment.NewLine;
            textBlock.Text = textBlock.Text + $"{DepDate.Text}" +
                            (rbRTrip.IsChecked == true ? " - " + $"{RetDate.SelectedDate:d}" : "") + Environment.NewLine;
            textBlock.Text = textBlock.Text + "Class: " + cbClass.SelectedItem;

            ChangeGrid(gridFirst, gridSecond, ToLeft);
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            ChangeGrid(gridSecond, gridFirst, ToRight);
        }

        private void rbTrip_Checked(object sender, RoutedEventArgs e)
        {
            if (lblRetDate.IsEnabled == false)
            {
                lblRetDate.IsEnabled = true;
                RetDate.IsEnabled = true;
            }
            else
            {
                lblRetDate.IsEnabled = false;
                RetDate.IsEnabled = false;
            }

        }

        private void RetDate_CalendarOpened(object sender, RoutedEventArgs e)
        {
            if (DepDate.SelectedDate != null)
            {
                RetDate.BlackoutDates.Add(new CalendarDateRange(DateTime.MinValue, DepDate.SelectedDate.Value));
                RetDate.SelectedDate = DepDate.SelectedDate.Value.AddDays(1);
            }

        }

        private void DepDate_CalendarOpened(object sender, RoutedEventArgs e)
        {
            DepDate.BlackoutDates.AddDatesInPast();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (tbFName.Text.Length == 0)
            {
                message("First Name is empty!");
                return;
            }
            if (tbSName.Text.Length == 0)
            {
                message("Second name is empty!");
                return;
            }
            if (tbPassp.Text.Length == 0)
            {
                message("Passport is empty!");
                return;
            }

            gridFirst.Dispatcher.Invoke(() =>

            {

                var connstr = ConfigurationManager.ConnectionStrings["connstring"].ToString();
                var dataContext = new TicketDataContext(connstr);

                var ticket = new Ticket
                {
                    Id = Guid.NewGuid(),
                    FirstName = tbFName.Text,
                    SecondName = tbSName.Text,
                    Src = (from c in dataContext.Cities where c.Name == cbFrom.SelectionBoxItem.ToString() select c.Id).First(),
                    Dst = (from c in dataContext.Cities where c.Name == cbTo.SelectionBoxItem.ToString() select c.Id).First(),
                    Way = rbTrip.IsChecked == true ? (byte)0 : (byte)1,
                    Class = cbClass.Text,
                    Date1 = (DateTime)DepDate.SelectedDate,
                    Date2 = RetDate.SelectedDate
                };

                dataContext.Tickets.InsertOnSubmit(ticket);
                dataContext.SubmitChanges();

            });

            #region coment

            //var connect = SqlConn();
            //connect.Open();
            //var command = connect.CreateCommand();
            //command.CommandText = $"INSERT INTO [dbo].[Ticket] " +
            //                      $"(FirstName, SecondName, Src, Dst, " +
            //                      $"Class, Way, Date1, Date2, Price) VALUES (" +
            //                      $"'{tbFName.Text}'," +
            //                      $"'{tbSName.Text}'," +
            //                      $"(SELECT (Id) FROM [dbo].[City] WHERE (Name='{cbFrom.Text}'))," +
            //                      $"(SELECT (Id) FROM [dbo].[City] WHERE (Name='{cbTo.Text}'))," +
            //                      $"'{cbClass.Text}'," +
            //                      $"{rt}," +
            //                      $"'{DepDate.DisplayDate:yyyy-MM-dd HH:mm:ss}'," +
            //                      $"'{RetDate.DisplayDate:yyyy-MM-dd HH:mm:ss}'," +
            //                      $"200)";
            //if (command.ExecuteNonQuery()>0)
            //    message("Done!");
            //connect.Close();

            #endregion
        }

        private void TabTickets_MouseEnter(object sender, MouseEventArgs e)
        {

            new Thread(() =>
            {
                var connstr = ConfigurationManager.ConnectionStrings["connstring"].ToString();
                var dataContext = new TicketDataContext(connstr);

                var res =
                    from t in dataContext.Tickets
                    select new
                    {
                        t.FirstName,
                        t.SecondName,
                        FromName = t.City.Name,
                        DstName = t.City1.Name,
                        t.Class,
                        t.Price,
                        t.Date1,
                        t.Date2
                    };

                TicketsGrid.Dispatcher.Invoke(() =>
                {
                    TicketsGrid.ItemsSource = res;
                });
            }).Start();
        }
    }
}
