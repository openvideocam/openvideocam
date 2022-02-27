using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using Windows.Storage.Pickers;
using Windows.UI;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using System.Diagnostics; //Proccess
using System.IO; //File access
using System.Linq;
using System.Reflection;
using System.Text;
using WinRT;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Documents;

using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenVCam.DataAccess;
using openvcamlibnet;
using System.Drawing.Imaging;

namespace openvcamapp.Pages
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InputVideoPage : Page
    {
        public InputVideoPage()
        {
            this.InitializeComponent();
        }

        public VideoMetaDataModel ViewModel 
        { 
            get
            {
                return VideoMetaDataModelSingleton.Instance;
            }
        }

        private void GridView_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            Grid grid = (Grid)sender;
            allContactsMenuFlyout.ShowAt(grid, e.GetPosition(grid));
            var a = ((FrameworkElement)e.OriginalSource).DataContext;
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

        private void AddVideoClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();

            IntPtr windowHandle = (App.Current as App).WindowHandle;

            var initializeWithWindow = openPicker.As<IInitializeWithWindow>();
            initializeWithWindow.Initialize(windowHandle);

            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".mp4");

            Task<StorageFile> file_async = openPicker.PickSingleFileAsync().AsTask();
            file_async.Wait();
            StorageFile file = file_async.Result;
            
            if (file != null)
            {

                this.ViewModel.MetaData.Videos.Add(new Video(file.Path, -1));
            }
        }

        private void RemoveVideoClick(object sender, RoutedEventArgs e)
        {
            int idx = VideosGridView.SelectedIndex;
            ViewModel.MetaData.Videos.RemoveAt(idx);
        }

        [ComImport]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }

        private void VideosGridView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void VideosGridView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var storageFile = items[0] as StorageFile;
                    //var bitmapImage = new BitmapImage();
                    //bitmapImage.SetSource(await storageFile.OpenAsync(FileAccessMode.Read));
                    // Set the image on the main page to the dropped image

                    if (storageFile != null)
                    {
                        ViewModel.MetaData.Videos.Add(new Video(storageFile.Path, -1));
                    }
                }
            }
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            string video_name = ((Video)((Grid)((Microsoft.UI.Xaml.Controls.Image)sender).Parent).DataContext).FileName;
            System.Drawing.Image snapshot = OpenVCamLib.GetSnapshotFromVideo(video_name);

            var stream = new System.IO.MemoryStream();
            snapshot.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            var snapshot_bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            snapshot_bitmap.SetSource(stream.AsRandomAccessStream());

            ((Microsoft.UI.Xaml.Controls.Image)sender).Source = snapshot_bitmap;
        }

        private async void SetCameraPosition_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogResult result = await setCameraPositionDiag.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // Terms of use were accepted.
            }
            else
            {
                // User pressed Cancel, ESC, or the back arrow.
                // Terms of use were not accepted.
            }
        }

        private void ProcessStatistics_Click(object sender, RoutedEventArgs e)
        {

        }

        private void setCameraPositionDiag_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {

        }
    }
}
