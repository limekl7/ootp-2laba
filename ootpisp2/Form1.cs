using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace ootpisp
{
    public partial class Form1 : Form
    {
        private Stage stage;
        private Random rand = new Random();
        private Timer timer;
        private bool isFullScreen = false;
        private Rectangle windowedBounds;
        private Panel customMenu;
        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.Width = 800;
            this.Height = 600;

            stage = new Stage(this.ClientSize.Width, this.ClientSize.Height);
            InitializeGame();

            this.KeyPreview = true; // Чтобы форма получала события клавиш
            this.KeyDown += Form1_KeyDown;

            this.Resize += Form1_Resize;

            timer = new Timer();
            timer.Interval = 6; // ~165 FPS
            timer.Tick += (s, e) =>
            {
                stage.Update();
                this.Invalidate();
            };
            timer.Start();

            InitializeCustomMenu();
        }

        private void InitializeCustomMenu()
        {
            customMenu = new Panel
            {
                Size = new Size(200, 280),
                Location = new Point((this.ClientSize.Width - 200) / 2, (this.ClientSize.Height - 280) / 2),
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            Button newGameButton = new Button
            {
                Text = "Новая игра",
                Size = new Size(160, 40),
                Location = new Point(20, 20),
            };
            newGameButton.Click += NewGame_Click;

            Button saveGameButton = new Button
            {
                Text = "Сохранить игру",
                Size = new Size(160, 40),
                Location = new Point(20, 70),
            };
            saveGameButton.Click += SaveGame_Click;

            Button loadGameButton = new Button
            {
                Text = "Загрузить игру",
                Size = new Size(160, 40),
                Location = new Point(20, 120),
            };
            loadGameButton.Click += LoadGame_Click;

            Button settingsButton = new Button
            {
                Text = "Настройки",
                Size = new Size(160, 40),
                Location = new Point(20, 170),
            };
            settingsButton.Click += Settings_Click;

            Button exitButton = new Button
            {
                Text = "Выход",
                Size = new Size(160, 40),
                Location = new Point(20, 220),
            };
            exitButton.Click += Exit_Click;

            customMenu.Controls.Add(newGameButton);
            customMenu.Controls.Add(saveGameButton);
            customMenu.Controls.Add(loadGameButton);
            customMenu.Controls.Add(settingsButton);
            customMenu.Controls.Add(exitButton);

            this.Controls.Add(customMenu);
            customMenu.BringToFront();
        }

        public void InitializeGame()
        {
            stage.ClearObjects();
            for (int i = 0; i < stage.GetObjects().Length; i++)
            {
                stage.AddObject(new Circle(
                    rand.Next(50, this.Width - 50),
                    rand.Next(50, this.Height - 50)));
            }
        }

        private void NewGame_Click(object sender, EventArgs e)
        {
            InitializeGame();
            customMenu.Visible = false;
        }

        private void SaveGame_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "JSON files (*.json)|*.json";
                sfd.DefaultExt = "json";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string json = JsonConvert.SerializeObject(stage.GetObjects(), Formatting.Indented, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects
                    });
                    File.WriteAllText(sfd.FileName, json);
                }
            }
            customMenu.Visible = false;
        }

        private void LoadGame_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "JSON files (*.json)|*.json";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string json = File.ReadAllText(ofd.FileName);
                    var loadedObjects = JsonConvert.DeserializeObject<List<DisplayObject>>(json, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects
                    });

                    stage.ClearObjects();
                    foreach (var obj in loadedObjects)
                    {
                        stage.AddObject(obj);
                    }
                }
            }
            customMenu.Visible = false;
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            using (SettingsForm settingsForm = new SettingsForm(stage, this))
            {
                settingsForm.ShowDialog(this);
            }
            customMenu.Visible = false;
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
            customMenu.Visible = false;
        }

    private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F: // Переключение полноэкранного режима
                    ToggleFullScreen();
                    break;

                case Keys.P: // Пауза
                    timer.Stop();
                    break;

                case Keys.R: // Продолжение
                    stage.DisableAccelerationForAll();
                    timer.Start();
                    break;

                case Keys.A: // Ускорение
                    stage.ToggleAccelerationForAll();
                    timer.Start();
                    break;

                case Keys.M: // Вызов меню
                    customMenu.Visible = !customMenu.Visible;
                    break;
            }
        }

        private void ToggleFullScreen()
        {
            if (!isFullScreen)
            {
                windowedBounds = new Rectangle(this.Location, this.Size);
                this.FormBorderStyle = FormBorderStyle.None; // Убираем рамку
                this.TopMost = true;
                this.Bounds = Screen.PrimaryScreen.Bounds;
                isFullScreen = true;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable; // Возвращаем рамку
                this.TopMost = false;
                this.Bounds = windowedBounds;
                isFullScreen = false;
            }
            // Обновляем размеры поля
            stage.Resize(this.ClientSize.Width, this.ClientSize.Height);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // Обновляем размеры игрового поля при изменении окна
            stage.Resize(this.ClientSize.Width, this.ClientSize.Height);
            customMenu.Location = new Point((this.ClientSize.Width - customMenu.Width) / 2, (this.ClientSize.Height - customMenu.Height) / 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            stage.Draw(e.Graphics);
        }
    }

    public class SettingsForm : Form
    {
        private ComboBox backgroundTypeComboBox;
        private Button colorButton;
        private Button gradientButton;
        private Button imageButton;
        private Stage stage;
        private Button changeBackground;
        private Button changeAmount;
        private Button applyChangeAmount;
        private NumericUpDown objectCountUpDown;
        private Form1 form;

        public SettingsForm(Stage gameStage, Form1 form)
        {
            this.stage = gameStage;
            this.form = form;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Настройки";
            this.Size = new Size(330, 130);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            changeBackground = new Button { Text="Изменить фон", Width = 120, Location = new Point((this.ClientSize.Width - 120) / 2, 10) };
            changeAmount = new Button { Text = "Изменить количество фигур", Width = 180, Location = new Point((this.ClientSize.Width - 180) / 2, 50) };

            changeBackground.Click += ChangeBackground_Click;
            changeAmount.Click += ChangeAmount_Click;

            this.Controls.Add(changeBackground);
            this.Controls.Add(changeAmount);
        }

        private void ChangeAmount_Click(object sender, EventArgs e)
        {
            this.Size = new Size(330, 160);
            changeAmount.Visible = false;
            changeBackground.Visible = false;

            Label label = new Label { Text="Количество фигур:", TextAlign = ContentAlignment.MiddleCenter, Width = 110, Height = 20, Location = new Point((this.ClientSize.Width - 110) / 2, 10) };
            objectCountUpDown = new NumericUpDown
            {
                Location = new Point((this.ClientSize.Width - 50) / 2, 35),
                Minimum = 1,
                Maximum = 300,
                Value = stage.GetObjects().Length,
                Width = 50
            };
            applyChangeAmount = new Button { Text = "Применить", Size = new Size(120, 30), Location = new Point((this.ClientSize.Width - 120) / 2, 70) };

            applyChangeAmount.Click += ApplyChangeAmount_Click;

            this.Controls.Add(applyChangeAmount);
            this.Controls.Add(label);
            this.Controls.Add(objectCountUpDown);
        }

        private void ApplyChangeAmount_Click(object sender, EventArgs e)
        {
            int newCount = (int)objectCountUpDown.Value;
            stage.ChangeAmount(newCount);
            form.InitializeGame();
        }

        private void ChangeBackground_Click(object sender, EventArgs e)
        {
            changeAmount.Visible = false;
            changeBackground.Visible = false;
            InitializeChangeBackground();
        }

        private void InitializeChangeBackground()
        {
            this.Text = "Настройки";
            this.Size = new Size(300, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            Label typeLabel = new Label { Text = "Тип фона:", Location = new Point(10, 10), AutoSize = true };
            backgroundTypeComboBox = new ComboBox
            {
                Location = new Point(10, 30),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            backgroundTypeComboBox.Items.AddRange(new string[] { "Сплошной цвет", "Градиент", "Изображение" });
            backgroundTypeComboBox.SelectedIndex = 0; // По умолчанию сплошной цвет

            colorButton = new Button { Text = "Выбрать цвет", Location = new Point(10, 60), Width = 120 };
            gradientButton = new Button { Text = "Выбрать градиент", Location = new Point(140, 60), Width = 120 };
            imageButton = new Button { Text = "Выбрать изображение", Location = new Point(10, 90), Width = 120 };

            colorButton.Click += ColorButton_Click;
            gradientButton.Click += GradientButton_Click;
            imageButton.Click += ImageButton_Click;
            backgroundTypeComboBox.SelectedIndexChanged += BackgroundTypeComboBox_SelectedIndexChanged;

            this.Controls.Add(typeLabel);
            this.Controls.Add(backgroundTypeComboBox);
            this.Controls.Add(colorButton);
            this.Controls.Add(gradientButton);
            this.Controls.Add(imageButton);

            UpdateButtonVisibility();
        }

        private void BackgroundTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonVisibility();
        }

        private void UpdateButtonVisibility()
        {
            colorButton.Visible = backgroundTypeComboBox.SelectedIndex == 0; // Сплошной цвет
            gradientButton.Visible = backgroundTypeComboBox.SelectedIndex == 1; // Градиент
            imageButton.Visible = backgroundTypeComboBox.SelectedIndex == 2; // Изображение
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    stage.SetBackgroundColor(colorDialog.Color);
                    this.Owner.Invalidate(); // Перерисовываем основную форму
                }
            }
        }

        private void GradientButton_Click(object sender, EventArgs e)
        {
            using (ColorDialog colorDialog1 = new ColorDialog())
            {
                if (colorDialog1.ShowDialog() == DialogResult.OK)
                {
                    using (ColorDialog colorDialog2 = new ColorDialog())
                    {
                        if (colorDialog2.ShowDialog() == DialogResult.OK)
                        {
                            stage.SetBackgroundGradient(colorDialog1.Color, colorDialog2.Color);
                            this.Owner.Invalidate();
                        }
                    }
                }
            }
        }

        private void ImageButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image files (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    stage.SetBackgroundImage(ofd.FileName);
                    this.Owner.Invalidate();
                }
            }
        }
    }
}
