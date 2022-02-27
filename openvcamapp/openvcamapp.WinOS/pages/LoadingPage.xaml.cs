﻿using openvcam.WinOS;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace openvcamapp.winos.pages
{
    /// <summary>
    /// Interaction logic for LoadingPage.xaml
    /// </summary>
    public partial class LoadingPage : Page
    {
        MainWindow m_parent;

        public LoadingPage(MainWindow parent)
        {
            m_parent = parent;

            m_parent.pnlNavBar.Visibility = Visibility.Hidden;

            InitializeComponent();
        }
    }
}
