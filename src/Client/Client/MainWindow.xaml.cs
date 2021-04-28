using System;
using System.Collections.Generic;
using System.Media;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CSocket client = null;
        string clientUsername = null;
        int clientIconID = -1;
        string selectedUsername = null;
        string selectedGroupSettingsName = null;
        static readonly string[] iconsNames =  {"user", "user (1)", "user (2)", "user (3)", "astronaut", "boy", "bride", "fashion-blogger", "faun", "gamer (1)", "gamer (2)", "gamer", "grandmother", "influencer", "jesus",
        "man (1)", "man (2)", "man (3)", "man (4)", "man (5)", "man", "ninja", "profile (1)", "profile", "student", "teenager", "witch (1)", "witch", "woman (1)", "woman (2)", "woman (3)", "woman (4)", "woman (5)", "woman", "worker"};

        bool closingEnd = false;
        bool errorEnd = false;

        public MainWindow()
        {

            InitializeComponent();
            LoginButton.IsEnabled = false;
            RegisterButton.IsEnabled = false;
            Connect();
        }

        #region WPF Events

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginOrRegisterStackPanel.Visibility = Visibility.Collapsed;
            LoginStackPanel.Visibility = Visibility.Visible;
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            LoginOrRegisterStackPanel.Visibility = Visibility.Collapsed;
            RegisterStackPanel.Visibility = Visibility.Visible;
        }

        private void LoginUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsValidString(LoginTextBox.Text) && IsValidString(UserPasswordBox.Password))
            {
                string username = LoginTextBox.Text.Trim();
                string password = UserPasswordBox.Password.Trim();
                client.SendInt32((int)ServerEvents.Login);
                client.SendString(username);
                client.SendString(password);
            }

            LoginTextBox.Text = "";
            UserPasswordBox.Password = "";
        }

        private void RegisterUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsValidString(RegisterUsernameTextBox.Text)  && IsValidString(RegisterPasswordBox.Password))
            {
                string username = RegisterUsernameTextBox.Text.Trim();
                string password = RegisterPasswordBox.Password.Trim();
                client.SendInt32((int)ServerEvents.Register);
                client.SendString(username);
                client.SendString(password);
            }

            RegisterUsernameTextBox.Text = "";
            RegisterPasswordBox.Password = "";
        }

        private void SignOutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            mySong.Stop();
            client.SendInt32((int)ServerEvents.Logout);
            ConversationListBox.Items.Clear();
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (ClientsListBox.SelectedItem == null) return;
                if (IsValidString(MessageTextBox.Text))
                {
                    SendMessage(MessageTextBox.Text.Trim(), ((ListBoxItem)ClientsListBox.SelectedItem).Tag.ToString());
                    ConversationListBox.Items.MoveCurrentToLast();
                    ConversationListBox.ScrollIntoView(ConversationListBox.Items.CurrentItem);
                }
                MessageTextBox.Text = "";
            }
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsListBox.SelectedItem == null) return;
            if (IsValidString(MessageTextBox.Text))
            {
                SendMessage(MessageTextBox.Text.Trim(), ((ListBoxItem)ClientsListBox.SelectedItem).Tag.ToString());
                ConversationListBox.Items.MoveCurrentToLast();
                ConversationListBox.ScrollIntoView(ConversationListBox.Items.CurrentItem);
            }
            MessageTextBox.Text = "";
        }

        private void CreateGroupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainDockPanel.Visibility = Visibility.Collapsed;
            GroupStackPanel.Visibility = Visibility.Visible;
            client.SendInt32((int)ServerEvents.GetUsers);
        }

        SoundPlayer mySong = new SoundPlayer(Client.Properties.Resources.Everlong);
        private void PlayMusicMenuItem_Click(object sender, RoutedEventArgs e)
        {
            mySong.Play();
        }
        private void StopMusicMenuItem_Click(object sender, RoutedEventArgs e)
        {
            mySong.Stop();
        }

        private void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoginStackPanel.Visibility == Visibility.Visible)
            {
                LoginStackPanel.Visibility = Visibility.Collapsed;
                LoginOrRegisterStackPanel.Visibility = Visibility.Visible;
            }
            else if (RegisterStackPanel.Visibility == Visibility.Visible)
            {
                RegisterStackPanel.Visibility = Visibility.Collapsed;
                LoginOrRegisterStackPanel.Visibility = Visibility.Visible;

            }
            else if (GroupStackPanel.Visibility == Visibility.Visible)
            {
                GroupStackPanel.Visibility = Visibility.Collapsed;
                MainDockPanel.Visibility = Visibility.Visible;
            }
            else if (SettingsStackPanel.Visibility == Visibility.Visible)
            {
                SettingsStackPanel.Visibility = Visibility.Collapsed;
                MainDockPanel.Visibility = Visibility.Visible;
            }
            else if (GroupSettingsStackPanel.Visibility == Visibility.Visible)
            {
                GroupSettingsStackPanel.Visibility = Visibility.Collapsed;
                MainDockPanel.Visibility = Visibility.Visible;
                NewGroupNameTextBox.Text = "";
                selectedGroupSettingsName = null;
                AddtoGroupUsers.Clear();
                RemoveFromGroupUsers.Clear();
            }
        }

        //Selected user changed
        private void ClientsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem selectedItem = (ListBoxItem)ClientsListBox.SelectedItem;
            if (selectedItem == null)
            {
                selectedUsername = null;
                return;
            }

            string newSelectedUsername = selectedItem.Tag.ToString();
            if (selectedUsername == newSelectedUsername) return;
            if (String.IsNullOrEmpty(newSelectedUsername))
            {
                ClientsListBox.SelectedItem = ClientsListBox.Items[ClientsListBox.SelectedIndex - 1];
                ClientsListBox.Items.Remove(selectedItem);
                newSelectedUsername = ((ListBoxItem)ClientsListBox.SelectedItem).Tag.ToString();
            }
            else
            {
                if (ClientsListBox.SelectedIndex != ClientsListBox.Items.Count - 1)
                {
                    if (String.IsNullOrEmpty(((ListBoxItem)ClientsListBox.Items[ClientsListBox.SelectedIndex + 1]).Tag.ToString()))
                    {
                        ClientsListBox.Items.RemoveAt(ClientsListBox.SelectedIndex + 1);
                    }
                }
            }
            selectedUsername = newSelectedUsername;
            client.SendInt32((int)ServerEvents.GetConversation);
            client.SendString(selectedUsername);
        }

        private void CreateGroupButtom_Click(object sender, RoutedEventArgs e)
        {
            if (AddtoGroupUsers.Count == 0) ShowError("You must choose minimum one user to your group");
            if (String.IsNullOrEmpty(GroupNameTextBox.Text)) return;
            client.SendInt32((int)ServerEvents.AddGroup);
            client.SendString(GroupNameTextBox.Text);
            client.SendInt32(AddtoGroupUsers.Count);
            foreach (string name in AddtoGroupUsers)
            {
                client.SendString(name);
            }
            GroupNameTextBox.Text = "";
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ConversationListBox.SelectedItem == null) return;
            client.SendInt32((int)ServerEvents.RemoveMessage);
            client.SendString(((ListBoxItem)ClientsListBox.SelectedItem).Tag.ToString());
            client.SendInt32(int.Parse(((ListBoxItem)ConversationListBox.SelectedItem).Tag.ToString()));
            ConversationListBox.Items.Remove(ConversationListBox.SelectedItem);

        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainDockPanel.Visibility = Visibility.Collapsed;
            SettingsStackPanel.Visibility = Visibility.Visible;
        }

        private void SetLoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsValidString(SetLoginTextBox.Text))
            {
                client.SendInt32((int)ServerEvents.ChangeLogin);
                client.SendString(SetLoginTextBox.Text.Trim());
                SetLoginTextBox.Text = "";

            }
        }

        private void SetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsValidString(SetPasswordTextBox.Password))
            {
                client.SendInt32((int)ServerEvents.ChangePassword);
                client.SendString(SetPasswordTextBox.Password.Trim());

                SetPasswordTextBox.Password = "";
            }
        }

        private void SetIcondButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (RadioButton icon in IconsWrapPanel.Children)
            {
                if ((bool)icon.IsChecked)
                {
                    int iconIndex = IconsWrapPanel.Children.IndexOf(icon);
                    client.SendInt32((int)ServerEvents.ChangeIcon);
                    client.SendInt32(iconIndex);
                }
            }
        }

        private void SetGroupNameButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsValidString(NewGroupNameTextBox.Text))
            {
                client.SendInt32((int)ServerEvents.ChangeGroupName);
                client.SendString(selectedGroupSettingsName);
                client.SendString(NewGroupNameTextBox.Text.Trim());
                NewGroupNameTextBox.Text = "";
            }
        }

        private void DeleteMembersButton_Click(object sender, RoutedEventArgs e)
        {
            if (RemoveFromGroupUsers.Count == 0) return;
            client.SendInt32((int)ServerEvents.RemoveGroupMember);
            client.SendString(selectedGroupSettingsName);
            client.SendInt32(RemoveFromGroupUsers.Count);
            foreach (string username in RemoveFromGroupUsers)
            {
                client.SendString(username);
            }
            AddtoGroupUsers.Clear();
            RemoveFromGroupUsers.Clear();
            client.SendInt32((int)ServerEvents.GetAddGroupUsers);
            client.SendString(selectedGroupSettingsName);
            client.SendInt32((int)ServerEvents.GetGroupMembers);
            client.SendString(selectedGroupSettingsName);
        }

        private void AddMembersButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddtoGroupUsers.Count == 0) return;
            client.SendInt32((int)ServerEvents.AddGroupMemeber);
            client.SendString(selectedGroupSettingsName);
            client.SendInt32(AddtoGroupUsers.Count);
            foreach (string username in AddtoGroupUsers)
            {
                client.SendString(username);
            }
            AddtoGroupUsers.Clear();
            RemoveFromGroupUsers.Clear();
            client.SendInt32((int)ServerEvents.GetAddGroupUsers);
            client.SendString(selectedGroupSettingsName);
            client.SendInt32((int)ServerEvents.GetGroupMembers);
            client.SendString(selectedGroupSettingsName);
        }

        private void GroupSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {

            ListBoxItem selectedItem = (ListBoxItem)ClientsListBox.SelectedItem;
            if (selectedItem == null) return;
            string name = selectedItem.Tag.ToString();
            if (name[name.Length - 1] == '&')
            {
                selectedGroupSettingsName = name;
                NewGroupNameTextBox.Text = "";
                MainDockPanel.Visibility = Visibility.Collapsed;
                GroupSettingsStackPanel.Visibility = Visibility.Visible;
                client.SendInt32((int)ServerEvents.GetAddGroupUsers);
                client.SendString(selectedGroupSettingsName);
                client.SendInt32((int)ServerEvents.GetGroupMembers);
                client.SendString(selectedGroupSettingsName);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mySong.Stop();
            if (client == null) return;
            if (errorEnd) return;
            closingEnd = true;
            client.SendInt32((int)ServerEvents.End);
        }

        #endregion


        #region Create XAML elements

        #region Main Panel
        private ListBoxItem GetUserListBoxItemByUsername(string username)
        {
            ListBoxItem userItem = null;
            foreach (ListBoxItem item in ClientsListBox.Items)
            {
                if (item.Tag.ToString() == username)
                {
                    userItem = item;
                    break;
                }
            }
            return userItem;
        }

        private void CreateNewContact(string username, int id, int insert = -1)
        {

            //values are scaled by 1.5
            //This what the code below creates:
            //< ListBoxItem Height = "40" Style = "{DynamicResource ClientsListBoxItemStyle}" IsSelected = "True" >

            //      < StackPanel Orientation = "Horizontal" >

            //            < Image Source = "/Images/avatar.jpg" Width = "20"  Margin = "5" />

            //            < TextBlock Foreground = "White" FontSize = "15" Margin = "5" > Piotr Sum </ TextBlock >

            //      </ StackPanel >

            //</ ListBoxItem >

            Image image = new Image();
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            Debug(iconsNames[id]);
            bi.UriSource = new Uri($"/Avatars/{iconsNames[id]}.png", UriKind.Relative);
            bi.EndInit();
            image.Source = bi;
            image.Width = 30;
            image.Margin = new Thickness(7);

            TextBlock textBlock = new TextBlock();
            textBlock.FontSize = 22;
            textBlock.Margin = new Thickness(7);
            textBlock.Foreground = Brushes.White;
            textBlock.Text = username.Substring(0, username.Length - 1);

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(textBlock);

            ListBoxItem item = new ListBoxItem();
            item.Content = stackPanel;
            item.Height = 60;
            item.SetResourceReference(ListBoxItem.StyleProperty, "ClientsListBoxItemStyle");
            item.Tag = username;
            if (insert >= 0) ClientsListBox.Items.Insert(insert, item);
            else ClientsListBox.Items.Add(item);

        }


        private void ChangeContactName(string oldUsername, string newUsername, int avatarID)
        {
            ListBoxItem item = GetUserListBoxItemByUsername(oldUsername);

            int index = ClientsListBox.Items.IndexOf(item);
            ClientsListBox.Items.Remove(item);
            CreateNewContact(newUsername, avatarID, index);
            if (ClientsListBox.SelectedItem == null) ClientsListBox.SelectedItem = GetUserListBoxItemByUsername(newUsername);
            client.SendInt32((int)ServerEvents.GetConversation);
            string newSelectedUsername = ((ListBoxItem)ClientsListBox.SelectedItem).Tag.ToString();
            client.SendString(newSelectedUsername);
        }

        private void ChangeIcon(string username, int newIconID)
        {
            ListBoxItem item = GetUserListBoxItemByUsername(username);
            int index = ClientsListBox.Items.IndexOf(item);
            ClientsListBox.Items.Remove(item);
            CreateNewContact(username, newIconID, index);
            if (ClientsListBox.SelectedItem == null) ClientsListBox.SelectedItem = GetUserListBoxItemByUsername(username);

        }



        private void SetUserToTop(string username)
        {
            ListBoxItem userItem = GetUserListBoxItemByUsername(username);
            if (ClientsListBox.Items.IndexOf(userItem) == 0) return;
            if (userItem == null) return;
            int index = ClientsListBox.Items.IndexOf(userItem);
            if (index < ClientsListBox.Items.Count - 1)
            {
                if (String.IsNullOrEmpty(((ListBoxItem)ClientsListBox.Items[index + 1]).Tag.ToString()))
                {
                    ClientsListBox.Items.RemoveAt(index + 1);
                }
            }
            ClientsListBox.Items.Remove(userItem);
            ClientsListBox.Items.Insert(0, userItem);

        }

        private void AddMessage(Message message)
        {
            //message from other guy:
            //<ListBoxItem HorizontalAlignment="Left" MaxWidth="525">
            //    <DockPanel>
            //         <Image VerticalAlignment="top" Width="45" Source="/Avatars/user.png" Margin="5 5 5 0"/>
            //         <StackPanel>
            //              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left">
            //                   <TextBlock VerticalAlignment="Bottom" FontSize="17" Margin="3 0 7 -1" Foreground="White">Kamil</TextBlock>
            //                   <TextBlock VerticalAlignment="Bottom" FontSize="14" Foreground="#999B9F">07/04/2021 11:33</TextBlock>
            //              </StackPanel>
            //              <Border HorizontalAlignment="Left" Background="#595B5F" CornerRadius="0, 10, 10, 10" Margin="3">
            //                    <TextBlock FontSize="20" Padding="7" Foreground="#D7D5D9" TextWrapping="Wrap">No już rok trzymam kanapkę w plecaku. Za chwilę będę musiał wynieść na dwór, bo ten grzyb tak rośnie, że już nie mieści mi się w pokoju
            //                    </TextBlock>
            //              </Border>
            //          </StackPanel>
            //    </DockPanel>
            //</ListBoxItem>

            //your message:
            //< ListBoxItem HorizontalAlignment = "Right" MaxWidth = "525" >                                             
            //    < StackPanel >                                           
            //         < TextBlock Margin = "0 0 5 0" HorizontalAlignment = "Right" VerticalAlignment = "Bottom" FontSize = "14" Foreground = "#999B9F" > 07 / 04 / 2021 11:33 </ TextBlock >                                                         
            //         < Border HorizontalAlignment = "Right" Background = "#595B5F" CornerRadius = "10, 10, 0, 10" Margin = "3" >                                                          
            //              < TextBlock FontSize = "20" Padding = "7" Foreground = "#D7D5D9" TextWrapping = "Wrap" > </ TextBlock >                              
            //         </ Border >                                                             
            //    </ StackPanel >                                                           
            //</ ListBoxItem >


            //date
            TextBlock dateTextBlock = new TextBlock() { FontSize = 14, VerticalAlignment = VerticalAlignment.Bottom, Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x9b, 0x9f)) };
            dateTextBlock.Text = message.ReciveDate.ToString("dd/MM/yyyy HH:mm");

            //message
            TextBlock messageTextBlock = new TextBlock() { FontSize = 20, Padding = new Thickness(7), Foreground = new SolidColorBrush(Color.FromRgb(0xd7, 0xd5, 0xd9)), TextWrapping = TextWrapping.Wrap };
            messageTextBlock.Text = message.Content;

            Border border = new Border() { Background = new SolidColorBrush(Color.FromRgb(0x59, 0x5b, 0x5f)), Margin = new Thickness(3) };

            StackPanel mainStackPanel = new StackPanel();

            ListBoxItem messageListBoxItem = new ListBoxItem() { MaxWidth = 525 };

            if (message.AuthorUsername == clientUsername)
            {
                messageListBoxItem.HorizontalAlignment = HorizontalAlignment.Right;
                MenuItem deleteMenuItem = new MenuItem() { Header = "Delete message", FontSize = 15, Foreground = Brushes.White };
                deleteMenuItem.Icon = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/Client;component/Images/bin-icon.png")) };
                deleteMenuItem.Click += DeleteMenuItem_Click;
                messageListBoxItem.ContextMenu = new ContextMenu() { Background = new SolidColorBrush(Color.FromRgb(0x2f, 0x31, 0x36)) };
                messageListBoxItem.ContextMenu.Items.Add(deleteMenuItem);

                border.CornerRadius = new CornerRadius(10, 10, 0, 10);
                border.HorizontalAlignment = HorizontalAlignment.Right;
                dateTextBlock.Margin = new Thickness(0, 0, 5, 0);
                dateTextBlock.HorizontalAlignment = HorizontalAlignment.Right;

                mainStackPanel.Children.Add(dateTextBlock);


                border.Child = messageTextBlock;
                mainStackPanel.Children.Add(border);

                messageListBoxItem.Content = mainStackPanel;
            }
            else
            {
                messageListBoxItem.HorizontalAlignment = HorizontalAlignment.Left;
                border.CornerRadius = new CornerRadius(0, 10, 10, 10);
                border.HorizontalAlignment = HorizontalAlignment.Left;


                TextBlock nameTextBlock = new TextBlock() { VerticalAlignment = VerticalAlignment.Bottom, FontSize = 17, Margin = new Thickness(3, 0, 7, -1), Foreground = Brushes.White };
                nameTextBlock.Text = message.AuthorUsername;

                StackPanel infoStackPanel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };

                infoStackPanel.Children.Add(nameTextBlock);
                infoStackPanel.Children.Add(dateTextBlock);

                mainStackPanel.Children.Add(infoStackPanel);


                border.Child = messageTextBlock;
                mainStackPanel.Children.Add(border);


                Image friendAvatar = new Image() { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(5, 5, 5, 0), Width = 30 };
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri($"/Avatars/{iconsNames[message.AvatarId]}.png", UriKind.Relative);  //PIOTREK tutaj trzeba dać ten id ikonki użytkownika który ci wysyła wiadomość
                bi.EndInit();
                friendAvatar.Source = bi;

                DockPanel dockPanel = new DockPanel();
                dockPanel.Children.Add(friendAvatar);
                dockPanel.Children.Add(mainStackPanel);

                messageListBoxItem.Content = dockPanel;
            }

            messageListBoxItem.Tag = message.Id;
            ConversationListBox.Items.Add(messageListBoxItem);
        }

        private void RemoveMessage(string authorUsername, int id)
        {
            if (((ListBoxItem)ClientsListBox.SelectedItem).Tag.ToString() != authorUsername) return;
            foreach (ListBoxItem item in ConversationListBox.Items)
            {
                if (item.Tag.ToString() == id.ToString())
                {
                    ConversationListBox.Items.Remove(item);
                    break;
                }
            }
        }

        private void AddReceiveMessageInfo(string authorUsername)
        {
            /*< ListBoxItem >
                    < StackPanel  Margin = "15,0,0,0" >
 
                        < Polygon Points = "1,0 4.7,6 15,6" Stroke = "#4095B4" Fill = "#4095B4" Margin = "0.6,0,0,-0.6" />
        
                               < Border Margin = "5,0,0,0" CornerRadius = "0,3,3,3" Background = "#4095B4" >
             
                                         < TextBlock Padding = "2" FontSize = "10" Foreground = "White" > Masz nową wiadomość!</ TextBlock >
                      
                                 </ Border >
                      
                     </ StackPanel >
                      
            </ ListBoxItem >*/

            TextBlock text = new TextBlock();
            text.Padding = new Thickness(3);
            text.FontSize = 15;
            text.Foreground = Brushes.White;
            text.Text = "You have new message!";

            Border border = new Border();
            border.Margin = new Thickness(7, 0, 0, 0);
            border.CornerRadius = new CornerRadius(0, 3, 3, 3);
            border.Background = new SolidColorBrush(Color.FromRgb(0x40, 0x95, 0xb4));
            border.Child = text;

            Polygon polygon = new Polygon();
            PointCollection collection = new PointCollection();
            collection.Add(new Point(1.5, 0));
            collection.Add(new Point(7, 9));
            collection.Add(new Point(22, 9));
            polygon.Points = collection;
            polygon.Stroke = new SolidColorBrush(Color.FromRgb(0x40, 0x95, 0xb4));
            polygon.Fill = new SolidColorBrush(Color.FromRgb(0x40, 0x95, 0xb4));
            polygon.Margin = new Thickness(0.9, 0, 0, -0.9);

            StackPanel stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(22, 0, 0, 5);
            stackPanel.Children.Add(polygon);
            stackPanel.Children.Add(border);

            ListBoxItem item = new ListBoxItem();
            item.SetResourceReference(ListBoxItem.StyleProperty, "NewMessageListBoxTemplate");
            item.Tag = "";

            item.Content = stackPanel;
            int insertIndex = 0;
            foreach (ListBoxItem userItem in ClientsListBox.Items)
            {
                if (userItem.Tag.ToString() == authorUsername)
                {
                    insertIndex = ClientsListBox.Items.IndexOf(userItem) + 1;
                    break;
                }
            }
            if (insertIndex < ClientsListBox.Items.Count)
            {
                ClientsListBox.Items.Insert(insertIndex, item);
            }
            else
            {
                ClientsListBox.Items.Add(item);
            }

        }

        private void SetUserList(List<(string, int)> users)
        {
            ClientsListBox.Items.Clear();
            foreach ((string name, int avatarID) data in users)
            {
                CreateNewContact(data.name, data.avatarID);
            }
            ListBoxItem firstItem = (ListBoxItem)ClientsListBox.Items[0];
            firstItem.IsSelected = true;
        }



        private void SetConversation(List<Message> conversation)
        {
            ConversationListBox.Items.Clear();
            foreach (Message message in conversation)
            {
                AddMessage(message);
            }
            ConversationListBox.Items.MoveCurrentToLast();
            ConversationListBox.ScrollIntoView(ConversationListBox.Items.CurrentItem);
        }
        #endregion

        #region CreateGroup Panel
        List<string> AddtoGroupUsers = new List<string>();
        List<string> RemoveFromGroupUsers = new List<string>();
        private void CreateGroupUsersListItem(string username, bool isCreateGroupPanel, bool addUserItem)
        {
            /*< ListBoxItem >

                < CheckBox Content = "Kowalski" Tag = "Login" Foreground = "#D7D5D9" Margin = "2" />

            </ ListBoxItem >*/

            CheckBox checkBox = new CheckBox();
            checkBox.Content = username;
            checkBox.Foreground = new SolidColorBrush(Color.FromRgb(0xd7, 0xd5, 0xd9));
            checkBox.Margin = new Thickness(3);
            checkBox.Checked += addUserItem ? new RoutedEventHandler(AddNewUserItemChecked) : new RoutedEventHandler(RemoveNewUserItemChecked);
            checkBox.Unchecked += addUserItem ? new RoutedEventHandler(AddNewUserItemUnchecked) : new RoutedEventHandler(RemoveNewUserItemUnchecked);
            checkBox.FontSize = 20;
            ListBoxItem item = new ListBoxItem();
            item.Content = checkBox;
            item.Tag = username;

            if (isCreateGroupPanel) ChooseFriendsListBox.Items.Add(item);
            else
            {
                if (addUserItem)
                    AddGroupMembersListBox.Items.Add(item);
                else
                    DeleteGroupMembersListBox.Items.Add(item);
            }
        }

        private void GroupSettingsSetUsersList(List<String> users, bool isCreateGroupPanel, bool isAddItemPanel)
        {
            ChooseFriendsListBox.Items.Clear();
            if (isAddItemPanel) AddGroupMembersListBox.Items.Clear();
            else DeleteGroupMembersListBox.Items.Clear();
            foreach (string name in users)
            {
                CreateGroupUsersListItem(name.Substring(0, name.Length - 1), isCreateGroupPanel, isAddItemPanel);
            }
        }

        private void AddNewUserItemChecked(object sender, RoutedEventArgs e)
        {
            AddtoGroupUsers.Add(((CheckBox)sender).Content.ToString());
        }

        private void AddNewUserItemUnchecked(object sender, RoutedEventArgs e)
        {
            AddtoGroupUsers.Remove(((CheckBox)sender).Content.ToString());
        }

        private void RemoveNewUserItemChecked(object sender, RoutedEventArgs e)
        {
            RemoveFromGroupUsers.Add(((CheckBox)sender).Content.ToString());
        }

        private void RemoveNewUserItemUnchecked(object sender, RoutedEventArgs e)
        {
            RemoveFromGroupUsers.Remove(((CheckBox)sender).Content.ToString());
        }





        #endregion

        #endregion


        #region Server Functions

        public void Connect()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //socket.Connect("server", 42070);
                socket.Connect("127.0.0.1", 1232);
                LoginButton.IsEnabled = true;
                RegisterButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                //Komunikat o błędzie w połączeniu i pojawia się przycisk reconnect
                ShowError("Cannot connect to server");
                return;
            }
            client = new CSocket(socket);
            Thread t = new Thread(() => { ReceiveFromServerThread(); });
            t.Start();
        }

        private void SendMessage(string message, string targetUsername)
        {
            client.SendInt32((int)ServerEvents.SendMessage);
            client.SendString(targetUsername);
            client.SendString(message);
        }

        public void ReceiveMessage(string authorUsername, string message, int id, string senderUsername, int avatarId)
        {
            ListBoxItem authorItem = GetUserListBoxItemByUsername(authorUsername);
            if (authorItem == null) return;
            if (authorUsername == (ClientsListBox.SelectedItem == null ? null : ((ListBoxItem)ClientsListBox.SelectedItem).Tag.ToString()))
            {
                AddMessage(new Message(message, senderUsername.Substring(0, senderUsername.Length - 1), DateTime.Now, id, avatarId));
                if (authorUsername != ((ListBoxItem)ClientsListBox.Items[0]).Tag.ToString())
                {
                    SetUserToTop(authorUsername);
                    ClientsListBox.SelectedItem = GetUserListBoxItemByUsername(authorUsername);
                }
                ConversationListBox.Items.MoveCurrentToLast();
                ConversationListBox.ScrollIntoView(ConversationListBox.Items.CurrentItem);
            }
            else
            {
                //powiadomienie dźwiękowe
                if (authorUsername != ((ListBoxItem)ClientsListBox.Items[0]).Tag.ToString())
                {
                    SetUserToTop(authorUsername);
                }

                if (((ListBoxItem)ClientsListBox.Items[1]).Tag.ToString() != "") AddReceiveMessageInfo(authorUsername);
            }
        }

        public void ReceiveFromServerThread()
        {

            while (true)
            {
                ServerEvents eventType = (ServerEvents)client.ReceiveInt32();
                switch (eventType)
                {
                    case ServerEvents.End:
                        {
                            errorEnd = !closingEnd;
                            goto END_MAIN_LOOP;
                        }
                    case ServerEvents.Error:
                        {
                            string error = client.ReceiveString();
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { ShowError(error); }));
                            break;
                        }
                    case ServerEvents.Login:
                        {
                            LoginStackPanel.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { LoginStackPanel.Visibility = Visibility.Collapsed; }));
                            MainDockPanel.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { MainDockPanel.Visibility = Visibility.Visible; }));
                            clientUsername = client.ReceiveString();
                            UsernameTextBlock.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { UsernameTextBlock.Text = clientUsername; }));
                            clientIconID = client.ReceiveInt32();
                            UserIconImage.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { UserIconImage.Source = new BitmapImage(new Uri($"/Avatars/{iconsNames[clientIconID]}.png", UriKind.Relative)); }));
                            client.SendInt32((int)ServerEvents.GetUsersAndGroups);
                            break;
                        }
                    case ServerEvents.Register:
                        {
                            RegisterStackPanel.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { RegisterStackPanel.Visibility = Visibility.Collapsed; }));
                            MainDockPanel.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { MainDockPanel.Visibility = Visibility.Visible; }));
                            clientUsername = client.ReceiveString();
                            UsernameTextBlock.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { UsernameTextBlock.Text = clientUsername; }));
                            clientIconID = client.ReceiveInt32();
                            UserIconImage.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { UserIconImage.Source = new BitmapImage(new Uri($"/Avatars/{iconsNames[clientIconID]}.png", UriKind.Relative)); }));
                            client.SendInt32((int)ServerEvents.GetUsersAndGroups);
                            break;
                        }
                    case ServerEvents.Logout:
                        {
                            MainDockPanel.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { MainDockPanel.Visibility = Visibility.Collapsed; }));
                            LoginOrRegisterStackPanel.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { LoginOrRegisterStackPanel.Visibility = Visibility.Visible; }));
                            clientUsername = null;
                            clientIconID = -1;
                            break;
                        }
                    case ServerEvents.ReceiveMessage:
                        {
                            string authorUsername = client.ReceiveString();
                            string message = client.ReceiveString();
                            int id = client.ReceiveInt32();
                            int avatarId = client.ReceiveInt32();
                            bool isGroup = client.ReceiveBoolean();
                            string senderUsername = isGroup ? client.ReceiveString() : authorUsername;
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { ReceiveMessage(authorUsername, message, id, senderUsername, avatarId); }));
                            break;
                        }
                    case ServerEvents.GetConversation:
                        {
                            int length = client.ReceiveInt32();
                            List<Message> conversation = new List<Message>();
                            for (int i = 0; i < length; i++)
                            {
                                Message message = new Message();
                                message.Content = client.ReceiveString();
                                message.AuthorUsername = client.ReceiveString();
                                message.Id = client.ReceiveInt32();
                                message.AvatarId = client.ReceiveInt32();
                                message.ReciveDate = new DateTime(client.ReceiveInt32(), client.ReceiveInt32(), client.ReceiveInt32(), client.ReceiveInt32(), client.ReceiveInt32(), 0);
                                conversation.Add(message);
                            }
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { SetConversation(conversation); });
                            break;
                        }
                    case ServerEvents.GetUsers:
                        {
                            int usersListLength = client.ReceiveInt32();
                            List<string> usersList = new List<string>();
                            for (int i = 0; i < usersListLength; i++)
                            {
                                usersList.Add(client.ReceiveString());
                            }
                            if (usersListLength == 0) break;

                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { GroupSettingsSetUsersList(usersList, GroupStackPanel.Visibility == Visibility.Visible, true); });

                            break;
                        }
                    case ServerEvents.AddUser:
                        {
                            string newUsername = client.ReceiveString();
                            if (GroupSettingsStackPanel.Visibility == Visibility.Visible)
                            {
                                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { CreateGroupUsersListItem(newUsername.Substring(0, newUsername.Length - 1), false, true); });
                            }
                            else if (GroupStackPanel.Visibility == Visibility.Visible)
                            {
                                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { CreateGroupUsersListItem(newUsername.Substring(0, newUsername.Length - 1), true, true); });
                            }
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { CreateNewContact(newUsername, 0); });
                            break;
                        }
                    case ServerEvents.AddGroup:
                        {
                            GroupStackPanel.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { GroupStackPanel.Visibility = Visibility.Collapsed; }));
                            MainDockPanel.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { MainDockPanel.Visibility = Visibility.Visible; }));
                            break;
                        }
                    case ServerEvents.AddGroupMemeber:
                        {
                            string groupName = client.ReceiveString();
                            string username = client.ReceiveString();
                            if (selectedGroupSettingsName == groupName)
                            {
                                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                                {
                                    foreach(ListBoxItem item in AddGroupMembersListBox.Items) 
                                    {
                                        Debug(item.Tag.ToString());
                                        if (item.Tag.ToString() == username) 
                                        {
                                            AddGroupMembersListBox.Items.Remove(item);
                                            if (AddtoGroupUsers.Contains(username)) AddtoGroupUsers.Remove(username);
                                            break;
                                        }
                                    }

                                    CreateGroupUsersListItem(username, false, false);

                                });
                            }
                            break;
                        }
                    case ServerEvents.GetUsersAndGroups:
                        {
                            int usersAndGroupsListLength = client.ReceiveInt32();
                            List<(string, int)> UsersAndGroupsList = new List<(string, int)>();
                            for (int i = 0; i < usersAndGroupsListLength; i++)
                            {
                                UsersAndGroupsList.Add((client.ReceiveString(), client.ReceiveInt32()));
                            }
                            if (usersAndGroupsListLength == 0) break;
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { SetUserList(UsersAndGroupsList); });
                            break;
                        }
                    case ServerEvents.GetGroupMembers:
                        {
                            int usersListLength = client.ReceiveInt32();
                            List<string> usersList = new List<string>();
                            for (int i = 0; i < usersListLength; i++)
                            {
                                usersList.Add(client.ReceiveString());
                            }
                            if (usersListLength == 0) break;

                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { GroupSettingsSetUsersList(usersList, GroupStackPanel.Visibility == Visibility.Visible, false); });
                            break;
                        }
                    case ServerEvents.ChangeLogin:
                        {
                            string oldUsername = client.ReceiveString();
                            string newUsername = client.ReceiveString();
                            int avatarID = client.ReceiveInt32();
                            if(oldUsername.Substring(0, oldUsername.Length-1) == clientUsername && oldUsername[oldUsername.Length-1] == '*') 
                            {
                                clientUsername = newUsername.Substring(0, newUsername.Length - 1);
                                UsernameTextBlock.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { UsernameTextBlock.Text = clientUsername; });
                                break;
                            }

                            if (oldUsername[oldUsername.Length - 1] == '&')
                            {
                                if (selectedGroupSettingsName == oldUsername) selectedGroupSettingsName = newUsername;
                            }
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { ChangeContactName(oldUsername, newUsername, avatarID); });
                            break;
                        }
                    case ServerEvents.ChangeIcon:
                        {
                            string username = client.ReceiveString();
                            clientIconID = client.ReceiveInt32();
                            if(username == clientUsername + "*") 
                            {
                                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { UserIconImage.Source = new BitmapImage(new Uri($"/Avatars/{iconsNames[clientIconID]}.png", UriKind.Relative)); });
                                break;
                            }
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { ChangeIcon(username, clientIconID); });
                            break;
                        }
                    case ServerEvents.RemoveMessage:
                        {
                            string authorUsername = client.ReceiveString();
                            int messageID = client.ReceiveInt32();
                            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { RemoveMessage(authorUsername, messageID); });
                            break;
                        }
                    case ServerEvents.RemoveGroupMember:
                        {
                            string groupName = client.ReceiveString();
                            string username = client.ReceiveString();
                            if(username == clientUsername) 
                            {
                                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                                {
                                    ListBoxItem item = GetUserListBoxItemByUsername(groupName);
                                    if (item != null)
                                    {
                                        ClientsListBox.Items.Remove(item);
                                        ConversationListBox.Items.Clear();
                                    }
                                    if (GroupSettingsStackPanel.Visibility == Visibility.Visible)
                                    {
                                        if (selectedGroupSettingsName == groupName)
                                        {
                                            GroupSettingsStackPanel.Visibility = Visibility.Collapsed;
                                            MainDockPanel.Visibility = Visibility.Visible;
                                        }
                                    }
                                });
                            }
                            else 
                            {
                                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
                                {
                                    foreach (ListBoxItem item in  DeleteGroupMembersListBox.Items)
                                    {
                                        Debug(item.Tag.ToString());
                                        if (item.Tag.ToString() == username)
                                        {
                                            DeleteGroupMembersListBox.Items.Remove(item);
                                            if (RemoveFromGroupUsers.Contains(username)) RemoveFromGroupUsers.Remove(username);
                                            break;
                                        }
                                    }

                                    CreateGroupUsersListItem(username, false, true);

                                });
                            }
                            break;
                        }
                }
            }

        END_MAIN_LOOP:
            if (errorEnd) Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate { Close(); });
        }



        private void ShowError(string error)
        {
            if (LoginOrRegisterStackPanel.Visibility == Visibility.Visible)
            {
                LoginOrRegisterErrorTextBlock.Text = "❌ " + error;
                LoginOrRegisterErrorBorder.Visibility = Visibility.Visible;

                var hideErrorBar = Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { LoginOrRegisterErrorBorder.Visibility = Visibility.Collapsed; }));
                });
            }
            else if (LoginStackPanel.Visibility == Visibility.Visible)
            {
                LoginErrorTextBlock.Text = "❌ " + error;
                LoginErrorBorder.Visibility = Visibility.Visible;

                var hideErrorBar = Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { LoginErrorBorder.Visibility = Visibility.Collapsed; }));
                });
            }
            else if (RegisterStackPanel.Visibility == Visibility.Visible)
            {
                RegisterErrorTextBlock.Text = "❌ " + error;
                RegisterErrorBorder.Visibility = Visibility.Visible;

                var hideErrorBar = Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { RegisterErrorBorder.Visibility = Visibility.Collapsed; }));
                });
            }
            else if (GroupStackPanel.Visibility == Visibility.Visible)
            {
                GroupErrorTextBlock.Text = "❌ " + error;

                GroupErrorBorder.Visibility = Visibility.Visible;

                var hideErrorBar = Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { GroupErrorBorder.Visibility = Visibility.Collapsed; }));
                });
            }
            else if (SettingsStackPanel.Visibility == Visibility.Visible)
            {
                SettingsErrorTextBlock.Text = "❌ " + error;

                SettingsErrorBorder.Visibility = Visibility.Visible;

                var hideErrorBar = Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { SettingsErrorBorder.Visibility = Visibility.Collapsed; }));
                });
            }
            else if (GroupSettingsStackPanel.Visibility == Visibility.Visible)
            {
                GroupSettingsErrorTextBlock.Text = "❌ " + error;

                GroupSettingsErrorBorder.Visibility = Visibility.Visible;

                var hideErrorBar = Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { GroupSettingsErrorBorder.Visibility = Visibility.Collapsed; }));
                });
            }
        }




        #endregion


        private void Debug(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }


        private bool IsValidString(string data) 
        {
            if (data == null) return false;
            data = data.Trim();
            return data.Length > 0;
        }
    }

    public enum ServerEvents
    {
        End,
        Error,
        Register,
        Login,
        Logout,
        SendMessage,
        ReceiveMessage,
        GetConversation,
        AddUser,
        GetUsers,
        AddGroup,
        AddGroupMemeber,
        GetUsersAndGroups,
        GetAddGroupUsers,
        GetGroupMembers,
        ChangeLogin,
        ChangePassword,
        ChangeIcon,
        ChangeGroupName,
        RemoveMessage,
        RemoveGroupMember,
    }
}
