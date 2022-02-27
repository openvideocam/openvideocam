using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace openvcamapp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            
            defaultCell.Background = new SolidColorBrush(Color.FromArgb(120, 255, 0, 0));

            var selectedLanguage = ApplicationLanguages.PrimaryLanguageOverride;
            if (!string.IsNullOrEmpty(selectedLanguage))
            {
                string idiom = "English";
                if (selectedLanguage == "en-US")
                    idiom = "English";
                else if (selectedLanguage == "es-ES")
                    idiom = "Español";
                else if (selectedLanguage == "pt-BR")
                    idiom = "Português";

                this.Settings_Idiom.SelectedValue = idiom;
            }
            else
            {
                this.Settings_Idiom.SelectedValue = "English";
            }
        }

        private void Grid_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Grid grid = (Grid)sender;

            Grid parent_grid = (Grid)grid.Parent;

            foreach (UIElement c in parent_grid.Children)
            {
                if (c is Grid)
                    ((Grid)c).Background = new SolidColorBrush(Color.FromArgb(255, 169, 169, 169));
            }

            grid.Background = new SolidColorBrush(Color.FromArgb(120, 255, 0, 0));
        }

        private void Settings_Idiom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string idiom = e.AddedItems[0].ToString();
            string idiom_code = "en-US";
            if (idiom == "English")
                idiom_code = "en-US";
            else if (idiom == "Español")
                idiom_code = "es-ES";
            else if (idiom == "Português")
                idiom_code = "pt-BR";

            ApplicationLanguages.PrimaryLanguageOverride = idiom_code;            
        }
    }
}
