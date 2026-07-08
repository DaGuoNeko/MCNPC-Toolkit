using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Effects;

namespace NpcSkinMaker
{
    /// <summary>
    /// 卡片容器 — 仿 PCL MyCard
    /// 圆角，阴影，hover 时阴影加深
    /// 使用 UserControl 继承以避免 Border.Content 名域冲突
    /// 通过 CardContent 属性接收外部内容
    /// </summary>
    [ContentProperty("CardContent")]
    public partial class MyCard : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(MyCard),
                new PropertyMetadata("", OnTitleChanged));

        public static readonly DependencyProperty CardContentProperty =
            DependencyProperty.Register("CardContent", typeof(object), typeof(MyCard),
                new PropertyMetadata(null, OnCardContentChanged));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public object CardContent
        {
            get { return GetValue(CardContentProperty); }
            set { SetValue(CardContentProperty, value); }
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MyCard card = d as MyCard;
            if (card != null)
                card.LabTitle.Text = e.NewValue != null ? (e.NewValue.ToString() ?? "") : "";
        }

        private static void OnCardContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MyCard card = d as MyCard;
            if (card != null && card.MainContent != null)
                card.MainContent.Content = e.NewValue;
        }

        public MyCard()
        {
            InitializeComponent();
            MouseEnter += MyCard_MouseEnter;
            MouseLeave += MyCard_MouseLeave;
        }

        private void MyCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (ShadowEffect != null)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0.25,
                    Duration = System.TimeSpan.FromMilliseconds(90)
                };
                ShadowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, animation);
            }
        }

        private void MyCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (ShadowEffect != null)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0.08,
                    Duration = System.TimeSpan.FromMilliseconds(200)
                };
                ShadowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, animation);
            }
        }
    }
}
