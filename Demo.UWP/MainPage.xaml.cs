using Windows.UI.Xaml;

namespace Demo.UWP
{
    public sealed partial class MainPage
    {
        public static UIElement thisPage;

        public MainPage()
        {
            this.InitializeComponent();

            thisPage = this;

            LoadApplication(new Demo.App());
        }
    }
}
