using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BTVN5
{
    public partial class Form1 : Form
    {
        private string _currentFilePath = null; // null = văn bản mới

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            rtbContent.TextChanged += (s, e) => UpdateStatus();
            rtbContent.SelectionChanged += RtbContent_SelectionChanged;

            // ToolStrip: nút check kiểu chữ
            btnBold.CheckOnClick = true;
            btnItalic.CheckOnClick = true;
            btnUnderline.CheckOnClick = true;

            // Sự kiện nút / menu
            mnuNew.Click += (s, e) => DoNew();
            mnuOpen.Click += (s, e) => DoOpen();
            mnuSave.Click += (s, e) => DoSave();
            mnuExit.Click += (s, e) => this.Close();
            mnuFontDialog.Click += (s, e) => ShowFontDialog();

            btnNew.Click += (s, e) => DoNew();
            btnOpen.Click += (s, e) => DoOpen();
            btnSave.Click += (s, e) => DoSave();

            btnBold.Click += (s, e) => ToggleStyle(FontStyle.Bold, btnBold.Checked);
            btnItalic.Click += (s, e) => ToggleStyle(FontStyle.Italic, btnItalic.Checked);
            btnUnderline.Click += (s, e) => ToggleStyle(FontStyle.Underline, btnUnderline.Checked);

            cbFonts.SelectedIndexChanged += (s, e) => ApplyFontFamily(cbFonts.ComboBox?.Text);
            cbSizes.SelectedIndexChanged += (s, e) =>
            {
                if (float.TryParse(cbSizes.ComboBox?.Text, out float sz)) ApplyFontSize(sz);
            };

            // Cho phép gõ size tùy ý
            cbSizes.ComboBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && float.TryParse(cbSizes.ComboBox.Text, out float v))
                {
                    ApplyFontSize(v);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Đổ fonts hệ thống
            cbFonts.ComboBox.Items.Clear();
            foreach (FontFamily f in new InstalledFontCollection().Families)
                cbFonts.ComboBox.Items.Add(f.Name);

            // Đổ size 8..72 như đề gợi ý
            int[] sizes = { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
            cbSizes.ComboBox.Items.Clear();
            foreach (var s in sizes) cbSizes.ComboBox.Items.Add(s.ToString());

            // Mặc định Tahoma, 14
            cbFonts.ComboBox.Text = "Tahoma";
            cbSizes.ComboBox.Text = "14";
            rtbContent.Font = new Font("Tahoma", 14f, FontStyle.Regular);
            UpdateStatus();
        }

        // ================== Chức năng ==================
        private void DoNew()
        {
            if (ConfirmLoseChanges() == DialogResult.Cancel) return;
            rtbContent.Clear();
            _currentFilePath = null;
            cbFonts.ComboBox.Text = "Tahoma";
            cbSizes.ComboBox.Text = "14";
            rtbContent.Font = new Font("Tahoma", 14f, FontStyle.Regular);
            btnBold.Checked = btnItalic.Checked = btnUnderline.Checked = false;
            UpdateStatus();
        }

        private void DoOpen()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Mở tập tin";
                dlg.Filter = "Rich Text (*.rtf)|*.rtf|Text Files (*.txt)|*.txt|All files (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                        if (ext == ".rtf")
                            rtbContent.LoadFile(dlg.FileName, RichTextBoxStreamType.RichText);
                        else
                            rtbContent.Text = File.ReadAllText(dlg.FileName);

                        _currentFilePath = dlg.FileName;
                        this.Text = "Soạn thảo văn bản - " + Path.GetFileName(_currentFilePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể mở tập tin.\n" + ex.Message, "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            UpdateStatus();
        }

        private void DoSave()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "Lưu nội dung văn bản";
                    dlg.Filter = "Rich Text (*.rtf)|*.rtf|Text Files (*.txt)|*.txt";
                    dlg.AddExtension = true;
                    dlg.DefaultExt = "rtf";
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    _currentFilePath = dlg.FileName;
                }
            }

            try
            {
                string ext = Path.GetExtension(_currentFilePath).ToLowerInvariant();
                if (ext == ".rtf")
                    rtbContent.SaveFile(_currentFilePath, RichTextBoxStreamType.RichText);
                else
                    File.WriteAllText(_currentFilePath, rtbContent.Text);

                MessageBox.Show("Đã lưu văn bản thành công.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Text = "Soạn thảo văn bản - " + Path.GetFileName(_currentFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu tập tin.\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowFontDialog()
        {
            using (FontDialog fontDlg = new FontDialog())
            {
                fontDlg.ShowColor = true;
                fontDlg.ShowApply = true;
                fontDlg.ShowEffects = true;
                fontDlg.ShowHelp = true;
                fontDlg.Font = rtbContent.SelectionFont ?? rtbContent.Font;
                fontDlg.Color = rtbContent.SelectionColor;

                if (fontDlg.ShowDialog() != DialogResult.Cancel)
                {
                    rtbContent.SelectionColor = fontDlg.Color;
                    rtbContent.SelectionFont = fontDlg.Font;
                    // Đồng bộ lại combobox + nút B/I/U
                    cbFonts.ComboBox.Text = fontDlg.Font.Name;
                    cbSizes.ComboBox.Text = ((int)Math.Round(fontDlg.Font.Size)).ToString();
                    SyncStyleButtons(fontDlg.Font);
                }
            }
        }

        // =============== Áp font / style ===============
        private Font GetBaseSelectionFont()
        {
            return rtbContent.SelectionFont ?? rtbContent.Font;
        }

        private void ApplyFontFamily(string family)
        {
            if (string.IsNullOrWhiteSpace(family)) return;
            var cur = GetBaseSelectionFont();
            try
            {
                rtbContent.SelectionFont = new Font(family, cur.Size, cur.Style);
                cbFonts.ComboBox.Text = family;
            }
            catch
            {
                // ignore invalid font family
            }
        }

        private void ApplyFontSize(float size)
        {
            if (size <= 0) return;
            var cur = GetBaseSelectionFont();
            rtbContent.SelectionFont = new Font(cur.FontFamily, size, cur.Style);
            cbSizes.ComboBox.Text = ((int)Math.Round(size)).ToString();
        }

        private void ToggleStyle(FontStyle style, bool turnOn)
        {
            var cur = GetBaseSelectionFont();
            var newStyle = cur.Style;

            if (turnOn) newStyle |= style;
            else newStyle &= ~style;

            rtbContent.SelectionFont = new Font(cur, newStyle);
        }

        private void SyncStyleButtons(Font f)
        {
            btnBold.Checked = f.Style.HasFlag(FontStyle.Bold);
            btnItalic.Checked = f.Style.HasFlag(FontStyle.Italic);
            btnUnderline.Checked = f.Style.HasFlag(FontStyle.Underline);
        }

        private void RtbContent_SelectionChanged(object sender, EventArgs e)
        {
            var f = GetBaseSelectionFont();
            // Đồng bộ nút và combobox theo vùng chọn
            SyncStyleButtons(f);
            cbFonts.ComboBox.Text = f.Name;
            cbSizes.ComboBox.Text = ((int)Math.Round(f.Size)).ToString();
        }

        // =============== Trạng thái / tiện ích ===============
        private void UpdateStatus()
        {
            // Tổng số từ (tách theo khoảng trắng)
            var words = rtbContent.Text
                .Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            lblWords.Text = $"Tổng số từ: {words.Length}";
        }

        private DialogResult ConfirmLoseChanges()
        {
            // Bài lab không yêu cầu theo dõi “đã sửa hay chưa”.
            // Hỏi nhẹ trước khi xoá nội dung cho an toàn.
            if (rtbContent.TextLength == 0) return DialogResult.OK;
            return MessageBox.Show("Bạn có muốn tạo văn bản mới? Nội dung hiện tại sẽ bị xóa.",
                    "Xác nhận", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        }
    }
}
