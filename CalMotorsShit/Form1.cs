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
                FlashLogger.Info("当前图像时右侧-高位相机");
            }
            else
            {
                cameraID = 1;//前方向机
                FlashLogger.Info("当前图像时前侧-低位相机");
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

            imgInfo = CenterAndSlope.GetProductParamters(myImg.Bitmap, cameraID,165);
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
            //foreach (var item in CenterAndSlope.segmentLines)
            //{
            //    if (item.Values.Where(a=>a=="Up"))
            //    {
            //upLine = CenterAndSlope.segmentUpLines.Keys.ToArray()[0];
            //    }
            //    else if (item.Value == "Bottom")
            //    {
            bottomLine = CenterAndSlope.segmentBottomLines.Keys.ToArray()[0];
            foreach (var item in bottomLine)
            {
                File.AppendAllText("bottomLine.txt", "X = " + item.Key.X.ToString() + "\tY = " + item.Key.Y.ToString() + "\tID = " + item.Value.ToString() + "\r\n");
            }
            //    }
            //    else if (item.Value == "Right")
            //    {
            //rightLine = CenterAndSlope.segmentRightLines.Keys.ToArray()[0];
            //    }
            //    else
            //    {


            //leftLine = CenterAndSlope.segmentLeftLines.Keys.ToArray()[0];
            //foreach (var item in leftLine)
            //{
            //    File.AppendAllText("leftLine.txt", "X = " + item.Key.X.ToString() + "Y = " + item.Key.Y.ToString() + "ID = " + item.Value.ToString() + "\r\n");
            //}

            //    }
            //}

            FlashLogger.Info("Up-Num =>"+upLine.Count.ToString());
            FlashLogger.Info("Bottom-Num =>"+bottomLine.Count.ToString());
            FlashLogger.Info("Right-Num =>"+rightLine.Count.ToString());
            FlashLogger.Info("Left-Num =>"+leftLine.Count.ToString());
            CvInvoke.Circle(myImg, new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), 10, new MCvScalar(255, 25, 100), 10);
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
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 10, 255), 3);//Blue
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 10, 255), 3);
            }

            ////++++++++++++up++++++++++++++++++++
            //foreach (var item in CenterAndSlope.DividedContours.Where(a => a.Value == 0).ToDictionary(a => a.Key, a => a.Value))
            //{
            //    CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 0, 255), 3);//Red
            //    //CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 0, 255), 3);
            //}
            //foreach (var item in upLine.Where(a => a.Value == 1))
            //{
            //    CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 255), 3);//Yellow
            //    CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 255, 255), 3);
            //}
            //foreach (var item in upLine.Where(a => a.Value == 2))
            //{
            //    CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 5), 3);//Green
            //    CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 255, 5), 3);
            //}
            //foreach (var item in upLine.Where(a => a.Value == 3))
            //{
            //    CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 0, 0), 3);//Blue
            //    CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 0, 0), 3);
            //}
            //foreach (var item in upLine.Where(a => a.Value == 4))
            //{
            //    CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 120, 255), 3);//Blue
            //    CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 120, 255), 3);
            //}
            ////++++++++++++Right++++++++++++++++++++
            //foreach (var item in rightLine.Where(a => a.Value == 0))
            //{
            //    CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 0, 255), 3);//Red
            //    CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 0, 255), 3);
            //}
            //foreach (var item in rightLine.Where(a => a.Value == 1))
            //{
            //    CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 255), 3);//Yellow
            //    CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 255, 255), 3);
            //}
            //foreach (var item in rightLine.Where(a => a.Value == 2))
            //{
            //    CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(0, 255, 5), 3);//Green
            //    CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(0, 255, 5), 3);
            //}
            //foreach (var item in rightLine.Where(a => a.Value == 3))
            //{
            //    CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 0, 0), 3);//Blue
            //    CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 0, 0), 3);
            //}
            //foreach (var item in rightLine.Where(a => a.Value == 4))
            //{
            //    CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 120, 255), 3);//Blue
            //    CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 120, 255), 3);
            //}
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
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255, 0, 0), 3);
            }
            foreach (var item in leftLine.Where(a => a.Value == 4))
            {
                CvInvoke.Circle(myImg, new Point(item.Key.X, item.Key.Y), 5, new MCvScalar(255, 120, 255), 3);//Blue
                CvInvoke.Line(myImg, new Point(item.Key.X, item.Key.Y), new Point((int)imgInfo.CenterOfImg.X, (int)imgInfo.CenterOfImg.Y), new MCvScalar(255,120,255), 3);
            }

            #endregion
            for (int i = 0; i < imgInfo.MotorShift.Length; i++)
            { 
                textBox1.Text  += "#【" + i + "】" + imgInfo.MotorShift[i].ToString() + "\r\n";
            }
            //x=0
            int y0 = (int)(imgInfo.CenterOfImg.Y - Math.Tan(imgInfo.RotatedAngle / 180f * Math.PI) * imgInfo.CenterOfImg.X);
            int y5472= (int)(imgInfo.CenterOfImg.Y + Math.Tan(imgInfo.RotatedAngle / 180f * Math.PI) * (5472-imgInfo.CenterOfImg.X));
            CvInvoke.Line(myImg, new Point(0, y0), new Point(5472, y5472), new MCvScalar(23, 25, 200), 10);
            //y=0
            int x0 = (int)(imgInfo.CenterOfImg.X+ Math.Tan(imgInfo.RotatedAngle / 180f * Math.PI)*imgInfo.CenterOfImg.Y);
            int x3648 = (int)(imgInfo.CenterOfImg.X- Math.Tan(imgInfo.RotatedAngle / 180f * Math.PI) * (3648-imgInfo.CenterOfImg.Y));
            CvInvoke.Line(myImg, new Point(x0, 0), new Point(x3648, 3648), new MCvScalar(223, 25, 20), 10);


            //x=0
            int y0rect = (int)(imgInfo.RectCenterOfImg.Y - Math.Tan(imgInfo.RectRotatedAngle / 180f * Math.PI) * imgInfo.RectCenterOfImg.X);
            int y5472rect = (int)(imgInfo.RectCenterOfImg.Y + Math.Tan(imgInfo.RectRotatedAngle / 180f * Math.PI) * (5472 - imgInfo.RectCenterOfImg.X));
            CvInvoke.Line(myImg, new Point(0, y0rect), new Point(5472, y5472rect), new MCvScalar(223, 25, 20), 2);
            //y=0
            int x0rect = (int)(imgInfo.RectCenterOfImg.X + Math.Tan(imgInfo.RectRotatedAngle / 180f * Math.PI) * imgInfo.RectCenterOfImg.Y);
            int x3648rect = (int)(imgInfo.RectCenterOfImg.X - Math.Tan(imgInfo.RectRotatedAngle / 180f * Math.PI) * (3648 - imgInfo.RectCenterOfImg.Y));
            CvInvoke.Line(myImg, new Point(x0rect, 0), new Point(x3648rect, 3648), new MCvScalar(23, 25, 200), 10);

            FlashLogger.Info("X-Y均值计算质心：" + imgInfo.CenterOfImg.ToString() + "\r\n外接矩形中心：" + imgInfo.RectCenterOfImg.ToString() + "\r\nHu矩计算质心：" + imgInfo.GravityCenterOfImg.ToString() + "\r\n"); ;

            FlashLogger.Info("最小二乘法拟合直线斜率："+imgInfo.RotatedAngle.ToString()+"\r\n外接矩形斜率："+ imgInfo.RectRotatedAngle.ToString()+"\r\n");
            FlashLogger.Info("24电机位移\r\n"+textBox1.Text);
            pictureBox1.Image = myImg.ToBitmap();
            pictureBox1.Update();
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
