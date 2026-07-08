using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace NpcSkinMaker
{
    /// <summary>
    /// 模型添加/编辑对话框
    /// </summary>
    public partial class EditModelDialog : Window
    {
        private bool _isEditing;

        public EditModelDialog(ModelEntry entry)
        {
            InitializeComponent();

            _isEditing = entry != null;
            LabTitle.Text = _isEditing ? "编辑模型" : "添加模型";

            TxtCollisionW.Text = "0.6";
            TxtCollisionH.Text = "1.8";
            TxtSource.Text = "原版";

            BtnClose.Click += (s, e) => DialogAnimationHelper.PlayExitAnimationAndClose(this);
            BtnConfirm.Click += (s, e) => Confirm();
            BtnCancel.Click += (s, e) => { DialogResult = false; DialogAnimationHelper.PlayExitAnimationAndClose(this); };

            BtnBrowseGeo.Click += (s, e) => BrowseFile(TxtGeoPath, "geo.json 文件|*geo.json;*.json|所有文件|*.*");
            BtnBrowsePreview.Click += (s, e) => BrowseFile(TxtPreviewPath, "PNG 图片|*.png|所有文件|*.*");

            // identifier 预览
            TxtCustomName.Text = "";
            TxtCustomName.InnerBox.TextChanged += (s, e) => UpdateIdPreview();

            BtnAddTexture.Click += (s, e) => AddTextureRow("", "");
            BtnAddAnim.Click += (s, e) => AddAnimRow("");
            BtnAddSkin.Click += (s, e) => AddSkinRow(PanSkinList.Children.Count, "", "Minecraft");

            if (entry != null)
                LoadEntry(entry);

            DialogAnimationHelper.Setup(this);
        }

        private void UpdateIdPreview()
        {
            string name = TxtCustomName.GetText().Trim();
            if (!string.IsNullOrEmpty(name))
                LabIdPreview.Text = "-> customnpc:" + name + "_dlcnpc";
            else
                LabIdPreview.Text = "-> customnpc:_dlcnpc";
        }

        private void BrowseFile(MyTextBox target, string filter)
        {
            var ofd = new OpenFileDialog { Filter = filter };
            if (ofd.ShowDialog() == true)
                target.Text = ofd.FileName;
        }

        // ===== 贴图行 =====

        private void AddTextureRow(string path, string name)
        {
            var row = new Border
            {
                Padding = new Thickness(0, 4, 0, 4),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = (Brush)Application.Current.TryFindResource("ColorBrushGray6")
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            var nameBox = new MyTextBox { Hint = "名称（可选）", Margin = new Thickness(0, 0, 4, 0) };
            nameBox.Text = name;
            Grid.SetColumn(nameBox, 0);
            grid.Children.Add(nameBox);

            var pathBox = new MyTextBox { Hint = "文件路径", Margin = new Thickness(0, 0, 4, 0) };
            pathBox.Text = path;
            Grid.SetColumn(pathBox, 1);
            grid.Children.Add(pathBox);

            var chooseBtn = new MyButton { Text = "选择", Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(8, 3, 8, 3) };
            chooseBtn.Click += (s, e) =>
            {
                var ofd = new OpenFileDialog { Filter = "PNG 图片|*.png|所有文件|*.*" };
                if (ofd.ShowDialog() == true)
                {
                    string msg;
                    if (Utils.ValidatePngFile(ofd.FileName, out msg))
                        pathBox.Text = ofd.FileName;
                    else
                        MyMsgBox.Show("验证失败: " + msg, "错误", MyMsgBox.MsgType.Warning);
                }
            };
            Grid.SetColumn(chooseBtn, 2);
            grid.Children.Add(chooseBtn);

            var delBtn = new MyButton { Text = "删除", ColorType = MyButton.ColorState.Red, Padding = new Thickness(8, 3, 8, 3) };
            delBtn.Click += (s, e) => PanTextureList.Children.Remove(row);
            Grid.SetColumn(delBtn, 3);
            grid.Children.Add(delBtn);

            row.Child = grid;
            PanTextureList.Children.Add(row);
        }

        private List<ModelTexture> GetTextures()
        {
            var result = new List<ModelTexture>();
            foreach (var child in PanTextureList.Children)
            {
                var row = child as Border;
                if (row == null) continue;
                var grid = row.Child as Grid;
                if (grid == null || grid.Children.Count < 2) continue;
                var nameBox = grid.Children[0] as MyTextBox;
                var pathBox = grid.Children[1] as MyTextBox;
                string name = nameBox != null ? nameBox.GetText().Trim() : "";
                string path = pathBox != null ? pathBox.GetText().Trim() : "";
                if (!string.IsNullOrEmpty(path))
                    result.Add(new ModelTexture(name, path));
            }
            return result;
        }

        // ===== 动画行 =====

        private void AddAnimRow(string animName)
        {
            var row = new Border
            {
                Padding = new Thickness(0, 4, 0, 4),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = (Brush)Application.Current.TryFindResource("ColorBrushGray6")
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            var nameBox = new MyTextBox { Hint = "animation.xxx.yyy", Margin = new Thickness(0, 0, 4, 0) };
            nameBox.Text = animName;
            Grid.SetColumn(nameBox, 0);
            grid.Children.Add(nameBox);

            var delBtn = new MyButton { Text = "删除", ColorType = MyButton.ColorState.Red, Padding = new Thickness(8, 3, 8, 3) };
            delBtn.Click += (s, e) => PanAnimList.Children.Remove(row);
            Grid.SetColumn(delBtn, 1);
            grid.Children.Add(delBtn);

            row.Child = grid;
            PanAnimList.Children.Add(row);
        }

        private List<string> GetAnimations()
        {
            var result = new List<string>();
            foreach (var child in PanAnimList.Children)
            {
                var row = child as Border;
                if (row == null) continue;
                var grid = row.Child as Grid;
                if (grid == null || grid.Children.Count < 1) continue;
                var nameBox = grid.Children[0] as MyTextBox;
                string val = nameBox != null ? nameBox.GetText().Trim() : "";
                if (!string.IsNullOrEmpty(val))
                    result.Add(val);
            }
            return result;
        }

        // ===== 皮肤行 =====

        private void AddSkinRow(int skinId, string name, string by)
        {
            var row = new Border
            {
                Padding = new Thickness(0, 4, 0, 4),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = (Brush)Application.Current.TryFindResource("ColorBrushGray6")
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            var idBox = new MyTextBox { Margin = new Thickness(0, 0, 4, 0) };
            idBox.Text = skinId.ToString();
            Grid.SetColumn(idBox, 0);
            grid.Children.Add(idBox);

            var nameBox = new MyTextBox { Hint = "显示名称", Margin = new Thickness(0, 0, 4, 0) };
            nameBox.Text = name;
            Grid.SetColumn(nameBox, 1);
            grid.Children.Add(nameBox);

            var byBox = new MyTextBox { Hint = "作者", Margin = new Thickness(0, 0, 4, 0) };
            byBox.Text = by;
            Grid.SetColumn(byBox, 2);
            grid.Children.Add(byBox);

            var delBtn = new MyButton { Text = "删除", ColorType = MyButton.ColorState.Red, Padding = new Thickness(8, 3, 8, 3) };
            delBtn.Click += (s, e) => PanSkinList.Children.Remove(row);
            Grid.SetColumn(delBtn, 3);
            grid.Children.Add(delBtn);

            row.Child = grid;
            PanSkinList.Children.Add(row);
        }

        private List<ModelSkin> GetSkinList()
        {
            var result = new List<ModelSkin>();
            int idx = 0;
            foreach (var child in PanSkinList.Children)
            {
                var row = child as Border;
                if (row == null) continue;
                var grid = row.Child as Grid;
                if (grid == null || grid.Children.Count < 3) continue;
                var idBox = grid.Children[0] as MyTextBox;
                var nameBox = grid.Children[1] as MyTextBox;
                var byBox = grid.Children[2] as MyTextBox;
                int sid = idx;
                int.TryParse(idBox != null ? idBox.GetText().Trim() : "", out sid);
                string name = nameBox != null ? nameBox.GetText().Trim() : "";
                string by = byBox != null ? byBox.GetText().Trim() : "";
                result.Add(new ModelSkin(sid, name, by));
                idx++;
            }
            return result;
        }

        // ===== 加载/获取 =====

        private void LoadEntry(ModelEntry entry)
        {
            TxtDisplayName.Text = entry.DisplayName;
            TxtCustomName.Text = entry.CustomName;
            TxtSource.Text = entry.SourceLabel;
            TxtIdleAnim.Text = entry.IdleAnimation;
            TxtWalkAnim.Text = entry.WalkAnimation;
            TxtWalkaAnim.Text = entry.WalkaAnimation;
            TxtAttackAnim.Text = entry.AttackAnimation;
            TxtDeathAnim.Text = entry.DeathAnimation;
            TxtCollisionW.Text = entry.CollisionWidth.ToString();
            TxtCollisionH.Text = entry.CollisionHeight.ToString();
            ChkAttachables.IsChecked = entry.EnableAttachables;
            TxtGeoPath.Text = entry.GeoPath;
            TxtPreviewPath.Text = entry.PreviewImagePath;

            PanTextureList.Children.Clear();
            foreach (var t in entry.Textures)
                AddTextureRow(t.Path, t.Name);

            PanAnimList.Children.Clear();
            foreach (var a in entry.AnimationList)
                AddAnimRow(a);

            PanSkinList.Children.Clear();
            foreach (var s in entry.SkinList)
                AddSkinRow(s.SkinId, s.Name, s.By);

            UpdateIdPreview();
        }

        public ModelEntry GetEntry()
        {
            var e = new ModelEntry();
            e.DisplayName = TxtDisplayName.GetText().Trim();
            e.CustomName = TxtCustomName.GetText().Trim();
            e.SourceLabel = string.IsNullOrEmpty(TxtSource.GetText().Trim()) ? "原版" : TxtSource.GetText().Trim();
            e.IdleAnimation = TxtIdleAnim.GetText().Trim();
            e.WalkAnimation = TxtWalkAnim.GetText().Trim();
            e.WalkaAnimation = TxtWalkaAnim.GetText().Trim();
            e.AttackAnimation = TxtAttackAnim.GetText().Trim();
            e.DeathAnimation = TxtDeathAnim.GetText().Trim();

            double w = 0.6, h = 1.8;
            double.TryParse(TxtCollisionW.GetText().Trim(), out w);
            double.TryParse(TxtCollisionH.GetText().Trim(), out h);
            e.CollisionWidth = w;
            e.CollisionHeight = h;

            e.EnableAttachables = ChkAttachables.IsChecked == true;
            e.GeoPath = TxtGeoPath.GetText().Trim();
            e.PreviewImagePath = TxtPreviewPath.GetText().Trim();
            e.Textures = GetTextures();
            e.AnimationList = GetAnimations();
            e.SkinList = GetSkinList();
            return e;
        }

        private void Confirm()
        {
            var entry = GetEntry();
            string err;
            if (!entry.Validate(out err))
            {
                MyMsgBox.Show(err, "填写不完整", MyMsgBox.MsgType.Warning);
                return;
            }
            DialogResult = true;
        }
    }
}
