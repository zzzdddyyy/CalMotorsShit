using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace CalMotorsShit
{
    public partial class Form1 : Form
    {
        int cameraID;
        KHImageServices CenterAndSlope = new KHImageServices();
        KHImageServices.ImageInfo imgInfo = new KHImageServices.ImageInfo();

        public Form1()
        {
            InitializeComponent();
            FlashLogger.Instance().Register();
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            if (!(rdbFront.Checked || rdbRight.Checked))
            {
                MessageBox.Show("先选择使用的图片来源！", "取像提示");
                return;
            }
            else if (rdbRight.Checked)
            {
                cameraID = 0;//右侧相机
            }
            else
            {
                cameraID = 1;//前方向机
            }
            Image<Bgr, byte> myImg = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                myImg = new Image<Bgr, byte>(openFileDialog.FileName);
            }
            if (myImg == null)
            {
                return;
            }

            imgInfo = CenterAndSlope.GetProductParamters(myImg.Bitmap, cameraID,175);
            foreach (var item in imgInfo.ImageCorner)
            {
                CvInvoke.Circle(myImg, new Point((int)item.X, (int)item.Y),10, new MCvScalar(0, 255, 33),10);
            }
            #region 可视化
            //画点
            Dictionary<Point, int> upLine = new Dictionary<Point, int>();
            Dictionary<Point, int> bottomLine = new Dictionary<Point, int>();
            Dictionary<Point, int> rightLine = new Dictionary<Point, int>();
            Dictionary<Point, int> leftLine = new Dictionary<Point, int>();
            foreach (var item in CenterAndSlope.segmentLines)
            {
                if (item.Value == "Up")
                {
                    upLine = item.Key;
                }
                else if (item.Value == "Bottom")
                {
                    bottomLine = item.Key;
                }
                else if (item.Value == "Right")
                {
                    rightLine = item.Key;
                }
                else
                {
                    leftLine = item.Key;
                }
            }
            FlashLogger.Info("Up-Num =>"+upLine.Count.ToString());
            FlashLogger.Info("Bottom-Num =>"+bottomLine.Count.ToString());
            FlashLogger.Info("Right-Num =>"+rightLine.Count.ToString());
            FlashLogger.Info("Left-Num =>"+leftLine.Count.ToString());

            //=========BOTTOM==================
            foreach (var item in bottomLine.Where(a => a.Value == 0))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 0, 255), 3);//Red
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 0, 255), 3);
            }
            foreach (var item in bottomLine.Where(a => a.Value == 1))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 255), 3);//Yellow
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 255, 255), 3);
            }
            foreach (var item in bottomLine.Where(a => a.Value == 2))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 5), 3);//Green
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 255, 5), 3);
            }
            foreach (var item in bottomLine.Where(a => a.Value == 3))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 0, 0), 3);//Blue
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 0, 0), 3);
            }
            foreach (var item in bottomLine.Where(a => a.Value == 4))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 120, 255), 3);//Blue
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 120, 255), 3);
            }

            //++++++++++++up++++++++++++++++++++
            foreach (var item in CenterAndSlope.DividedContours.Where(a => a.Value == 0).ToDictionary(a => a.Key, a => a.Value))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 0, 255), 3);//Red
                //CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 0, 255), 3);
            }
            foreach (var item in upLine.Where(a => a.Value == 1))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 255), 3);//Yellow
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 255, 255), 3);
            }
            foreach (var item in upLine.Where(a => a.Value == 2))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 5), 3);//Green
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 255, 5), 3);
            }
            foreach (var item in upLine.Where(a => a.Value == 3))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 0, 0), 3);//Blue
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 0, 0), 3);
            }
            foreach (var item in upLine.Where(a => a.Value == 4))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 120, 255), 3);//Blue
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 120, 255), 3);
            }
            //++++++++++++Right++++++++++++++++++++
            foreach (var item in rightLine.Where(a => a.Value == 0))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 0, 255), 3);//Red
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 0, 255), 3);
            }
            foreach (var item in rightLine.Where(a => a.Value == 1))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 255), 3);//Yellow
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 255, 255), 3);
            }
            foreach (var item in rightLine.Where(a => a.Value == 2))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 5), 3);//Green
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 255, 5), 3);
            }
            foreach (var item in rightLine.Where(a => a.Value == 3))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 0, 0), 3);//Blue
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 0, 0), 3);
            }
            foreach (var item in rightLine.Where(a => a.Value == 4))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 120, 255), 3);//Blue
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 120, 255), 3);
            }
            //++++++++++++Left++++++++++++++++++++
            foreach (var item in leftLine.Where(a => a.Value == 0))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 0, 255), 3);//Red
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 0, 255), 3);
            }
            foreach (var item in leftLine.Where(a => a.Value == 1))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 255), 3);//Yellow
            }
            foreach (var item in leftLine.Where(a => a.Value == 2))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 5), 3);//Green
            }
            foreach (var item in leftLine.Where(a => a.Value == 3))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 0, 0), 3);//Blue
            }
            foreach (var item in leftLine.Where(a => a.Value == 4))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 120, 255), 3);//Blue
            }

            #endregion
            for (int i = 0; i < imgInfo.MotorShift.Length; i++)
            { 
                textBox1.Text  += "#【" + i + "】" + imgInfo.MotorShift[i].ToString() + "\r\n";
            }
            FlashLogger.Info(textBox1.Text);
            pictureBox1.Image = myImg.ToBitmap();
        }
        /// <summary>
        /// 获取ROI
        /// </summary>
        /// <param name="image">需裁剪的原图</param>
        /// <param name="rect">裁剪留下的ROI大小</param>
        /// <returns>ROI</returns>
        private Image<Gray, byte> GetROI(Image<Gray, byte> image,int cameraID)
        {
            Rectangle rightROI = new Rectangle(new Point(950, 50), new Size(4050 - 850, 2800));
            Rectangle frontROI = new Rectangle(new Point(720, 100), new Size(4300 - 720, 3400));
            //程序中image是原始图像，类型Image<Gray, byte>，rectangle是矩形，CropImage是截得的图像。
            Image<Gray, byte> resImag = image.CopyBlank();
            using (var mask = new Image<Gray, Byte>(image.Size))
            {
                mask.SetZero();//设置所有值为0
                if (cameraID==0)
                {
                    mask.ROI = rightROI;
                }
                else
                {
                    mask.ROI = frontROI;
                }
                mask.SetValue(255);//设置ROI的值为255
                mask.ROI = Rectangle.Empty;//去掉ROI
               //res(I)=img1(I)+img2(I) if mask(I)!=0
                CvInvoke.BitwiseAnd(image, mask, resImag);
            }
            return resImag;
        }

        private void btnDetect_Click(object sender, EventArgs e)
        {
            //显示二值化图：
            frmROI frmROI = new frmROI(CenterAndSlope.BinaryImage.Bitmap);
 
            frmROI.Show();
        }
    }
}
