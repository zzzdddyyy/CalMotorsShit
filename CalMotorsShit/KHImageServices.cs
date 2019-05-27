﻿///时间：2019/05/12-21/00
///地点：恒康现场
///Authentication：ZDY
///内容：此类包含了相机畸变校正、图像处理、角点识别、旋转角度获取、
///根据图像边长像素数量自适应计算24电机位移量

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;


namespace CalMotorsShit
{
    public class KHImageServices
    {
        //字段
        //public double RotatedAngle { get; set; }//记录旋转角
        //public PointF CenterOfRobot { get; set; }//记录机器人法兰中心点坐标
        //public PointF CenterOfImg { get; set; }//记录图像中心点坐标
        //public PointF[] ImageCorner { get; set; }//记录图像角点信息
        //public double AxisLong { get; set; }//记录长轴
        //public double AxisShort { get; set; }//记录短轴
        public Image<Gray, byte> BinaryImage{get;set;}//记录二值化图像
        public struct ImageInfo
        {
            public double RotatedAngle { get; set; }//记录旋转角
            public PointF CenterOfRobot { get; set; }//记录机器人法兰中心点坐标
            public PointF CenterOfImg { get; set; }//记录图像中心点坐标
            public PointF[] ImageCorner { get; set; }//记录图像角点信息
            public double AxisLong { get; set; }//记录长轴
            public double AxisShort { get; set; }//记录短轴
            public double[] MotorShift { get; set; }//记录--24--距离
        }
        //public Dictionary<Point, int> dividedCoutour;//记录四条边的点集
        //public Dictionary<Dictionary<Point, int>,string> segmentLine;//记录单边分5组--临时用
        #region 全局变量
        private const int width = 5472;      //相机分辨率 2000W像素
        private const int height = 3648;
        private Size imageSize = new Size(width, height);//图像的大小

        private Matrix<double> cameraMatrix = new Matrix<double>(3, 3);//相机内部参数
        private Matrix<double> distCoeffs = new Matrix<double>(5, 1);//畸变参数
        private Matrix<double> c2wFrontMatrix = new Matrix<double>(4, 4);//相机坐标系到Robot世界坐标系变换矩阵
        private Matrix<double> c2wRightMatrix = new Matrix<double>(4, 4);//相机坐标系到Robot世界坐标系变换矩阵

        private Matrix<float> mapx = new Matrix<float>(height, width); //x坐标对应的映射矩阵
        private Matrix<float> mapy = new Matrix<float>(height, width);
        private MCvTermCriteria criteria = new MCvTermCriteria(100, 1e-5);//求角点迭代的终止条件（精度）

        private Matrix<double> frontCameraTrans = new Matrix<double>(1, 6);
        private Matrix<double> rightCameraTrans = new Matrix<double>(1, 6);

        readonly Mat kernelClosing = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(-1, -1));//运算核
        #endregion

        /// <summary>
        /// 获取高架（右侧）相机矩阵和畸变矩阵，cameraID==0
        /// </summary>
        private void GetRightCamParams(int spongeH)
        {
            //填充相机矩阵
            cameraMatrix[0, 0] = 4946.18163894192;
            cameraMatrix[0, 1] = -0.624726287412256;
            cameraMatrix[0, 2] = 2744.28781898738;
            cameraMatrix[1, 0] = 0;
            cameraMatrix[1, 1] = 4945.59080271046;
            cameraMatrix[1, 2] = 1877.58021306542;
            cameraMatrix[2, 0] = 0;
            cameraMatrix[2, 1] = 0;
            cameraMatrix[2, 2] = 1;
            //填充畸变矩阵
            distCoeffs[0, 0] = -0.0710236695402012;//K1
            distCoeffs[1, 0] = 0.128020024828545;//K2
            distCoeffs[2, 0] = 0;//P1
            distCoeffs[3, 0] = 0;//P2
            distCoeffs[4, 0] = 0;//K3
            //填充坐标变换矩阵
            if (spongeH <= 15)
            {
                rightCameraTrans[0, 0] = -5.04228004e-03;
                rightCameraTrans[0, 1] = 6.97035198e-01;
                rightCameraTrans[0, 2] = -1.74467751e+03;
                rightCameraTrans[0, 3] = 6.95646602e-01;
                rightCameraTrans[0, 4] = 4.65284138e-03;
                rightCameraTrans[0, 5] = 6.91376326e+02;
            }
            else if (spongeH > 15 && spongeH <= 55)
            {
                rightCameraTrans[0, 0] = -5.52666766e-03;
                rightCameraTrans[0, 1] = 6.89446652e-01;
                rightCameraTrans[0, 2] = -1.72967079e+03;
                rightCameraTrans[0, 3] = 6.88204324e-01;
                rightCameraTrans[0, 4] = 4.45682892e-03;
                rightCameraTrans[0, 5] = 7.12227372e+02;
            }
            else if (spongeH > 55 && spongeH <= 75)
            {
                rightCameraTrans[0, 0] = -4.37142654e-03;
                rightCameraTrans[0, 1] = 6.85270616e-01;
                rightCameraTrans[0, 2] = -1.72347553e+03;
                rightCameraTrans[0, 3] = 6.83202904e-01;
                rightCameraTrans[0, 4] = 3.90862861e-03;
                rightCameraTrans[0, 5] = 7.25361627e+02;
            }
            else if (spongeH > 75 && spongeH <= 95)
            {
                rightCameraTrans[0, 0] = -4.53470345e-03;
                rightCameraTrans[0, 1] = 6.81258505e-01;
                rightCameraTrans[0, 2] = -1.71636378e+03;
                rightCameraTrans[0, 3] = 6.79263048e-01;
                rightCameraTrans[0, 4] = 3.83386933e-03;
                rightCameraTrans[0, 5] = 7.36451842e+02;
            }
            else if (spongeH > 95)
            {
                rightCameraTrans[0, 0] = -4.81150536e-03;
                rightCameraTrans[0, 1] = 6.79535937e-01;
                rightCameraTrans[0, 2] = -1.71339340e+03;
                rightCameraTrans[0, 3] = 6.77619641e-01;
                rightCameraTrans[0, 4] = 3.97985425e-03;
                rightCameraTrans[0, 5] = 7.40662060e+02;
            }
        }

        /// <summary>
        /// 获取前方相机矩阵和畸变矩阵，cameraID==1
        /// </summary>
        private void GetFrontCamParams(int spongeH)
        {
            //填充相机矩阵
            cameraMatrix[0, 0] = 4932.50425753357;
            cameraMatrix[0, 1] = -0.132857117137422;
            cameraMatrix[0, 2] = 2733.57870537282;
            cameraMatrix[1, 0] = 0;
            cameraMatrix[1, 1] = 4932.08299994076;
            cameraMatrix[1, 2] = 1816.99165090490;
            cameraMatrix[2, 0] = 0;
            cameraMatrix[2, 1] = 0;
            cameraMatrix[2, 2] = 1;
            //填充畸变矩阵,先径向再切向
            distCoeffs[0, 0] = -0.06603195967528847;//K1
            distCoeffs[1, 0] = 0.116813172417637;//K2
            distCoeffs[2, 0] = 0;//P1
            distCoeffs[3, 0] = 0;//P2
            distCoeffs[4, 0] = 0;//K3
            //填充坐标变换矩阵
            if (spongeH <= 60)
            {
                frontCameraTrans[0, 0] = 6.98343875e-01;
                frontCameraTrans[0, 1] = 4.35391944e-04;
                frontCameraTrans[0, 2] = 5.83221141e+02;
                frontCameraTrans[0, 3] = 4.02996368e-04;
                frontCameraTrans[0, 4] = -6.98756329e-01;
                frontCameraTrans[0, 5] = 1.13790727e+03;

            }
            else if (spongeH <= 175 && spongeH > 60)
            {
                frontCameraTrans[0, 0] = 6.73722735e-01;
                frontCameraTrans[0, 1] = 5.89894437e-04;
                frontCameraTrans[0, 2] = 6.51819702e+02;
                frontCameraTrans[0, 3] = 1.10845668e-03;
                frontCameraTrans[0, 4] = -6.74728521e-01;
                frontCameraTrans[0, 5] = 1.09239819e+03;
            }
            else if (spongeH <= 225 && spongeH > 175)
            {
                frontCameraTrans[0, 0] = 6.66144136e-01;
                frontCameraTrans[0, 1] = -3.51634805e-04;
                frontCameraTrans[0, 2] = 6.73332341e+02;
                frontCameraTrans[0, 3] = 3.98092828e-04;
                frontCameraTrans[0, 4] = -6.67180844e-01;
                frontCameraTrans[0, 5] = 1.08068911e+03;
            }
            else if (spongeH > 225)
            {
                frontCameraTrans[0, 0] = 6.49604925e-01;
                frontCameraTrans[0, 1] = -3.21692944e-04;
                frontCameraTrans[0, 2] = 7.18122346e+02;
                frontCameraTrans[0, 3] = 3.96075968e-04;
                frontCameraTrans[0, 4] = -6.50491543e-01;
                frontCameraTrans[0, 5] = 1.05098143e+03;
            }
        }
        /// <summary>
        /// 获取ROI
        /// </summary>
        /// <param name="image">需裁剪的原图</param>
        /// <param name="rect">裁剪留下的ROI大小</param>
        /// <returns>ROI</returns>
        private Image<Gray, byte> GetROI(Image<Gray, byte> image, Rectangle rect)
        {
            //程序中image是原始图像，类型Image<Gray, byte>，rectangle是矩形，CropImage是截得的图像。
            Image<Gray, byte> resImag = image.CopyBlank();
            using (var mask = new Image<Gray, Byte>(image.Size))
            {
                mask.SetZero();//设置所有值为0
                mask.ROI = rect;
                mask.SetValue(255);//设置ROI的值为255
                mask.ROI = Rectangle.Empty;//去掉ROI
                                           //res(I)=img1(I)+img2(I) if mask(I)!=0
                CvInvoke.BitwiseAnd(image, mask, resImag);
            }
            return resImag;
        }

        /// <summary>
        /// 获取产品轮廓，camera=0代表右侧相机，camera=1代表前方向机
        /// </summary>
        /// <param name="img">产品图像</param>
        /// <param name="cameraID">相机编号</param>
        /// <returns></returns>
        private List<VectorOfPoint> GetContours(Bitmap img,int cameraID,int spongeH)
        {
            #region 灰度处理
            //灰度化
            Image<Gray, byte>  grayImg = new Image<Gray, byte>(img).PyrDown().PyrUp();
            Image<Gray, byte> resImg = grayImg.CopyBlank();
            Image<Gray, byte>  remapImg = grayImg.CopyBlank();//映射后图像
              //获取畸变参数
            if (cameraID == 0)
            {
                GetRightCamParams(spongeH);
                resImg = GetROI(grayImg, new Rectangle(new Point(1070, 0), new Size(4330 - 1070, 3648)));
            }
            else
            {
                GetFrontCamParams(spongeH);
                resImg = GetROI(grayImg, new Rectangle(new Point(1150, 0), new Size(4390 - 1150, 3500 - 150)));
            }

            //畸变校正
            try
            {
                CvInvoke.InitUndistortRectifyMap(cameraMatrix, distCoeffs, null, cameraMatrix, imageSize, DepthType.Cv32F, mapx, mapy);
                CvInvoke.Remap(resImg, remapImg, mapx, mapy, Inter.Linear, BorderType.Reflect101, new MCvScalar(0));
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            //二值化
            Image<Gray, byte>  binaryImg = grayImg.CopyBlank();//创建一张和灰度图一样大小的画布
            
            CvInvoke.Threshold(remapImg, binaryImg, 0, 255, ThresholdType.Otsu);//控制是否需要畸变校正
            //传到字段
            BinaryImage = binaryImg;
            //Closing【去除闭运算20190125】
            Image<Gray, byte> closingImg = binaryImg.CopyBlank();//闭运算后图像
            CvInvoke.MorphologyEx(binaryImg, closingImg, MorphOp.Open, kernelClosing, new Point(-1, -1), 5, BorderType.Default, new MCvScalar(255, 0, 0, 255));
            #endregion
            List<VectorOfPoint> myContours = new List<VectorOfPoint>();//序号，轮廓
            try
            {
                #region 去除白色不相干区域块
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();//区块集合
                Image<Gray, byte> dnc = new Image<Gray, byte>(binaryImg.Width, binaryImg.Height);
                CvInvoke.FindContours(binaryImg, contours, dnc, RetrType.External, ChainApproxMethod.ChainApproxNone);//轮廓集合
                //CvInvoke.DrawContours(new Image<Bgr,byte>(img), contours, 0, new MCvScalar(0, 255, 255), 4);
                myContours.Clear();
                for (int k = 0; k < contours.Size; k++)
                {
                    double area = CvInvoke.ContourArea(contours[k]);//获取各连通域的面积 
                    if (area < 500000)//根据面积作筛选(指定最小面积,最大面积2500000):
                    {
                        CvInvoke.FillConvexPoly(binaryImg, contours[k], new MCvScalar(0));
                    }
                    if (area > 1000000)//3000000
                    {
                        myContours.Add(contours[k]);
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            #endregion
            return myContours;
        }

        /// <summary>
        /// 获取机器人法兰中心移动坐标和旋转角度，camera=0代表右侧相机，camera=1代表前方向机
        /// </summary>
        /// <param name="bitmap">产品图像</param>
        /// <param name="cameraID">相机编号</param>
        public ImageInfo GetProductParamters(Bitmap bitmap,int cameraID,int spongeH)
        {
            ImageInfo imageInfo = new ImageInfo();
            Dictionary<Point, int> dividedCoutour;//记录四条边的点集
            List<double> motorShif = new List<double>();
            List<VectorOfPoint> imageContours = GetContours(bitmap, cameraID, spongeH);
            VectorOfPoint productContour = imageContours.Max();
            Point[] pst = productContour.ToArray();//获取轮廓上所有的点集
            if (productContour!=null)
            {
                var minRect = CvInvoke.MinAreaRect(productContour); //最小外接矩形
                PointF[] pt = CvInvoke.BoxPoints(minRect);//最小外接矩形四个角点 
                PointF po = minRect.Center;//最小外接矩形中心
                double RotatedAngle = Math.Abs(minRect.Angle) > 45 ? minRect.Angle + 90 : minRect.Angle;
                //长轴,短轴,倾角计算:
                //AxisLong =  Math.Sqrt(Math.Pow(pt[1].X - pt[0].X, 2) + Math.Pow(pt[1].Y - pt[0].Y, 2));
                double AxisLong = minRect.Size.Width > minRect.Size.Height ? minRect.Size.Width : minRect.Size.Height;
                //AxisShort =  Math.Sqrt(Math.Pow(pt[2].X - pt[1].X, 2) + Math.Pow(pt[2].Y - pt[1].Y, 2));
                double AxisShort = minRect.Size.Height <= minRect.Size.Width ? minRect.Size.Height : minRect.Size.Width;
                imageInfo.ImageCorner = pt;
                imageInfo.CenterOfImg = po;
                imageInfo.RotatedAngle = RotatedAngle;
                imageInfo.AxisLong = AxisLong;
                imageInfo.AxisShort = AxisShort;
                #region 计算电气抓位移
                dividedCoutour = new Dictionary<Point, int>();//储存四条分组的边的点集

                for (int i = 0; i < pt.Length; i++)
                {
                    Point p1 = new Point((int)pt[i].X, (int)pt[i].Y);
                    Point p2 = new Point((int)pt[(i + 1) % 4].X, (int)pt[(i + 1) % 4].Y);
                    if (p1.X < width / 2 && p1.Y > height / 2)//左
                    {
                        foreach (var item in pst)
                        {
                            if (item.X <= (p1.X >= p2.X ? p1.X : p2.X) + 8 && item.X >= (p1.X > p2.X ? p2.X : p1.X) - 8 && item.Y <= (p1.Y >= p2.Y ? p1.Y : p2.Y) - 4 && item.Y >= (p1.Y > p2.Y ? p2.Y : p1.Y) + 4)
                            {
                                dividedCoutour[item] = 0;
                            }
                        }
                    }
                    else if (p1.X < width / 2 && p1.Y < height / 2)//上
                    {
                        foreach (var item in pst)
                        {
                            if (item.X <= (p1.X >= p2.X ? p1.X : p2.X) && item.X >= (p1.X > p2.X ? p2.X : p1.X) && item.Y <= (p1.Y >= p2.Y ? p1.Y : p2.Y) + 40 && item.Y >= (p1.Y > p2.Y ? p2.Y : p1.Y) - 10)
                            {
                                dividedCoutour[item] = 1;
                            }
                        }
                    }
                    else if (p1.X > width / 2 && p1.Y < height / 2)//右
                    {
                        foreach (var item in pst)
                        {
                            if (item.X <= (p1.X >= p2.X ? p1.X : p2.X) + 8 && item.X >= (p1.X > p2.X ? p2.X : p1.X) - 8 && item.Y <= (p1.Y >= p2.Y ? p1.Y : p2.Y) - 4 && item.Y >= (p1.Y > p2.Y ? p2.Y : p1.Y) + 4)
                            {
                                dividedCoutour[item] = 2;
                            }
                        }
                    }
                    else if (p1.X > width / 2 && p1.Y > height / 2)//下
                    {
                        foreach (var item in pst)
                        {
                            if (item.X <= (p1.X >= p2.X ? p1.X : p2.X) - 4 && item.X >= (p1.X > p2.X ? p2.X : p1.X) + 4 && item.Y <= (p1.Y >= p2.Y ? p1.Y : p2.Y) + 10 && item.Y >= (p1.Y > p2.Y ? p2.Y : p1.Y) - 10)
                            {
                                dividedCoutour[item] = 3;
                            }
                        }
                    }
                }
                //验证一段分5组：
                List<double> up = new List<double>();
                List<double> bottom = new List<double>();
                List<double> left = new List<double>();
                List<double> right = new List<double>();
                left = GetFiveDistanceOnLine(RotatedAngle, new Point((int)po.X, (int)po.Y), AxisLong, AxisShort, dividedCoutour.Where(a => a.Value == 0).ToDictionary(a => a.Key, a => a.Value));
                if (cameraID == 0)//R
                {
                    for (int i = 0; i < left.Count; i++)
                    {
                        if (left[i] > AxisLong * 3 / 5f)
                        {
                            left[i] = 0;
                        }
                    }
                }
                up = GetFiveDistanceOnLine(RotatedAngle, new Point((int)po.X, (int)po.Y), AxisLong, AxisShort, dividedCoutour.Where(a => a.Value == 1).ToDictionary(a => a.Key, a => a.Value));
                if (cameraID == 1)//F
                {
                    for (int i = 0; i < up.Count; i++)
                    {
                        if (up[i] > AxisLong * 3 / 5f)
                        {
                            up[i] = 0;
                        }
                    }
                }
                right = GetFiveDistanceOnLine(RotatedAngle, new Point((int)po.X, (int)po.Y), AxisLong, AxisShort, dividedCoutour.Where(a => a.Value == 2).ToDictionary(a => a.Key, a => a.Value));
                if (cameraID == 0)//R
                {
                    for (int i = 0; i < right.Count; i++)
                    {
                        if (right[i] > AxisLong * 3 / 5f)
                        {
                            right[i] = 0;
                        }
                    }
                }
                bottom = GetFiveDistanceOnLine(RotatedAngle, new Point((int)po.X, (int)po.Y), AxisLong, AxisShort, dividedCoutour.Where(a => a.Value == 3).ToDictionary(a => a.Key, a => a.Value));
                if (cameraID == 1)//F
                {
                    for (int i = 0; i < bottom.Count; i++)
                    {
                        if (bottom[i] > AxisLong * 3 / 5f)
                        {
                            bottom[i] = 0;
                        }
                    }
                }

                if (cameraID == 1)//F
                {
                    motorShif = left.Concat(up).Concat(right).Concat(bottom).ToList<double>();//左-上-右-下

                }
                else
                {
                    motorShif = bottom.Concat(left).Concat(up).Concat(right).ToList<double>();//下-左-上-右
                }
                double _20 = motorShif[19];
                double _21 = motorShif[9];
                double _22 = motorShif[8];
                double _23 = motorShif[18];
                motorShif.Add(_20);
                motorShif.Add(_21);
                motorShif.Add(_22);
                motorShif.Add(_23);
                imageInfo.MotorShift = motorShif.ToArray();
                #endregion

                //foreach (var item in dividedCoutour)
                //{
                //    File.AppendAllText("ContourPoints.txt", "X=" + item.Key.X.ToString() +"\t\t"+ "Y=" + item.Key.Y.ToString() +"\t\t"+"Key="+item.Value.ToString()+ "\n\r");
                //}



                Matrix<double> imgCenter = new Matrix<double>(3, 1)
                {
                    [0, 0] = po.X,
                    [1, 0] = po.Y,
                    [2, 0] = 1
                };
                //Matrix<double> cameraCenter = imgCenter.Inverse();
                if (cameraID == 0)
                {
                    float x = (float)(rightCameraTrans[0, 0] * imgCenter[0, 0] + rightCameraTrans[0, 1] * imgCenter[1, 0] + rightCameraTrans[0, 2]);
                    float y = (float)(rightCameraTrans[0, 3] * imgCenter[0, 0] + rightCameraTrans[0, 4] * imgCenter[1, 0] + rightCameraTrans[0, 5]);
                    imageInfo.CenterOfRobot = new PointF(x, y);
                }
                else
                {
                    float x = (float)(frontCameraTrans[0, 0] * imgCenter[0, 0] + frontCameraTrans[0, 1] * imgCenter[1, 0] + frontCameraTrans[0, 2]);
                    float y = (float)(frontCameraTrans[0, 3] * imgCenter[0, 0] + frontCameraTrans[0, 4] * imgCenter[1, 0] + frontCameraTrans[0, 5]);
                    imageInfo.CenterOfRobot = new PointF(x, y);
                }
            }

            return imageInfo;
        }

        /// <summary>
        /// 计算每条线上5个电机的移动距离
        /// </summary>
        /// <param name="angle">旋转角度</param>
        /// <param name="centerPoint">中心像素点坐标</param>
        /// <param name="AxisLong">长轴</param>
        /// <param name="AxisShort">短轴</param>
        /// <param name="linePoints"></param>
        /// <param name="pr"像素比</param>
        /// <returns></returns>
        private List<double> GetFiveDistanceOnLine(double angle,Point centerPoint, double AxisLong,double AxisShort, Dictionary<Point,int> linePoints, double pr = 1.66)
        {
            Dictionary<Dictionary<Point, int>, string> segmentLine;//记录单边分5组--临时用
            List<double> fiveD = new List<double>();
            List<Point> aLine = new List<Point>();
            Dictionary<Point, int>  segInnerLine  = new Dictionary<Point, int>();//临时做全局变量
            segmentLine = new Dictionary<Dictionary<Point, int>, string>();
            //水平中线-----相机最好与传送带有1度以上夹角
            double A_ = angle > -0.01 && angle < 0.01 ? 0 : Math.Tan(angle/180f*Math.PI);
            double B_ = -1;
            double C_ = centerPoint.Y - A_* centerPoint.X;
            //垂直中线
            double A1 = A_==0 ? -1 : (-1 / A_);
            double B1 = A_ == 0 ? 0:-1;
            double C1 = A_ == 0 ? centerPoint.X : centerPoint.Y - A1 * centerPoint.X;

            if (linePoints.Values.ToList()[0]%2==0)//||
            {
                Dictionary<Point, int> wholeLineOrderPoints = linePoints.OrderBy(a => a.Key.Y).ToDictionary(a => a.Key, a => a.Value);
                int count = wholeLineOrderPoints.Count;
                int mid = count / 2;

                int t0 = mid - (int)(416.5 * pr / 2);
                int t1 = mid + (int)(416.5* pr / 2);
                int t2 = t1 + (int)(433* pr);
                int t3 = t2 + (int)(333.75* pr);
                int t_1 = t0 - (int)(433* pr);
                int t_2 = t_1 - (int)(333.75 * pr);

                aLine = wholeLineOrderPoints.Keys.ToList();
                //TODO:把一段上的点按照上面的索引 分成5段
                //======================================
                //*90 | 225 | 460 | 500 | 460 | 225 | 90 |
                //*50 | 333.75 | 433 | 416.5 | 433 | 333.75 | 50
                //================================
                if (aLine[count / 2].X < width/ 2)//左
                {
                    if ((t_1 - 70 * pr) < 0)//最外排针刺不到棉
                    {
                        for (int i = 0; i < aLine.Count; i++)
                        {
                            if (i <= t1 && i >= t0)//中间
                            {
                                segInnerLine[aLine[i]] = 0;
                            }
                            else if (i <= t2 && i >= t1)//上一
                            {
                                segInnerLine[aLine[i]] = 1;
                            }
                            else if (i <= t0 && i >= t_1)//下一
                            {
                                segInnerLine[aLine[i]] = 2;
                            }
                        }
                        segInnerLine[new Point(9998, 9998)] = 3;
                        segInnerLine[new Point(9999, 9999)] = 4;
                    }
                    else
                    {
                        for (int i = 0; i < aLine.Count; i++)
                        {
                            if (i <= t1 && i >= t0)//中间
                            {
                                segInnerLine[aLine[i]] = 0;
                            }
                            else if (i <= t2 && i >= t1)//上一
                            {
                                segInnerLine[aLine[i]] = 1;
                            }
                            else if (i <= t0 && i >= t_1)//下一
                            {
                                segInnerLine[aLine[i]] = 2;
                            }
                            else if (i <= t3 && i >= t2)//上二
                            {
                                segInnerLine[aLine[i]] = 3;
                            }
                            else//下二
                            {
                                segInnerLine[aLine[i]] = 4;
                            }
                        }
                    }
                    segmentLine[segInnerLine] = "Left";
                }
                else//右
                {
                    if ((t_1 - 70 * pr) < 0)//最外排针刺不到棉
                    {
                        for (int i = 0; i < aLine.Count; i++)
                        {
                            if (i <= t1 && i >= t0)//中间
                            {
                                segInnerLine[aLine[i]] = 0;
                            }
                            else if (i <= t2 && i >= t1)//上一
                            {
                                segInnerLine[aLine[i]] = 1;
                            }
                            else if (i <= t0 && i >= t_1)//下一
                            {
                                segInnerLine[aLine[i]] = 2;
                            }
                        }
                        segInnerLine[new Point(9998, 9998)] = 3;
                        segInnerLine[new Point(9999, 9999)] = 4;
                    }
                    else
                    {
                        for (int i = 0; i < aLine.Count; i++)
                        {
                            if (i <= t1 && i >= t0)//中间
                            {
                                segInnerLine[aLine[i]] = 0;
                            }
                            else if (i <= t2 && i >= t1)//上一
                            {
                                segInnerLine[aLine[i]] = 1;
                            }
                            else if (i <= t0 && i >= t_1)//下一
                            {
                                segInnerLine[aLine[i]] = 2;
                            }
                            else if (i <= t3 && i >= t2)//上二
                            {
                                segInnerLine[aLine[i]] = 3;
                            }
                            else//下二
                            {
                                segInnerLine[aLine[i]] = 4;
                            }
                        }
                    }
                    segmentLine[segInnerLine] = "Right";
                }
                //TODO===计算List<double>存储5个线段距离中线的距离
                fiveD.Add(segInnerLine.Where(a => a.Value == 0).Select(a => a.Key).Average(a => {return GetPoint2LineDistance(a, A1, B1, C1); }));
                fiveD.Add(segInnerLine.Where(a => a.Value == 1).Select(a => a.Key).Average(a => {return GetPoint2LineDistance(a, A1, B1, C1); }));
                fiveD.Add(segInnerLine.Where(a => a.Value == 2).Select(a => a.Key).Average(a => {return GetPoint2LineDistance(a, A1, B1, C1); }));
                fiveD.Add(segInnerLine.Where(a => a.Value == 3).Select(a => a.Key).Average(a => {return GetPoint2LineDistance(a, A1, B1, C1); }));
                fiveD.Add(segInnerLine.Where(a => a.Value == 4).Select(a => a.Key).Average(a => {return GetPoint2LineDistance(a, A1, B1, C1); }));
            
            }
            else//=
            {
                Dictionary<Point, int> wholeLineOrderPoints = linePoints.OrderBy(a => a.Key.X).ToDictionary(a => a.Key, a => a.Value);
                int count = wholeLineOrderPoints.Count;
                int mid = count / 2;
                
                int t0 = mid - (int)(500 * pr / 2);
                int t1 = mid + (int)(500 * pr / 2);
                int t2 = t1 + (int)(460 * pr);
                int t3 = t2 + (int)(225 * pr);
                int t_1 = t0 - (int)(460 * pr);
                int t_2 = t_1 - (int)(225 * pr);
                //TODO:把一段上的点按照上面的索引 分成5段
                //======================================
                //*90 | 225 | 460 | 500 | 460 | 225 | 90 |
                //*50 | 333.75 | 433 | 416.5 | 433 | 333.75 | 50
                //================================

                aLine = wholeLineOrderPoints.Keys.ToList();
                if (aLine[count/2].Y<height/2)//上
                {
                    if ((t_1-70*pr)<0)//最外排针刺不到棉
                    {
                        for (int i = 0; i < aLine.Count; i++)
                        {
                            if (i <= t1 && i >= t0)//中间
                            {
                                segInnerLine[aLine[i]] = 0;
                            } 
                            else if (i <= t2 && i >= t1)//右一
                            {
                                segInnerLine[aLine[i]] = 1;
                            }
                            else if (i <= t0 && i >= t_1)//左一
                            {
                                segInnerLine[aLine[i]] = 2;
                            }
                        }
                        segInnerLine[new Point(9998, 9998)] = 3;
                        segInnerLine[new Point(9999, 9999)] = 4;
                    }
                    else
                    {
                        for (int i = 0; i < aLine.Count; i++)
                        {
                            if (i <= t1 && i >= t0)//中间
                            {
                                segInnerLine[aLine[i]] = 0;
                            }
                            else if (i <= t2 && i >= t1)//右一
                            {
                                segInnerLine[aLine[i]] = 1;
                            }
                            else if (i <= t0 && i >= t_1)//左一
                            {
                                segInnerLine[aLine[i]] = 2;
                            }
                            else if (i <= t3 && i >= t2)//右二
                            {
                                segInnerLine[aLine[i]] = 3;
                            }
                            else//左二
                            {
                                segInnerLine[aLine[i]] = 4;
                            }
                        }
                    }
                    segmentLine[segInnerLine] = "Up";
                }
                else//下
                {
                    if ((t_1 - 70 * pr) < 0)//最外排针刺不到棉
                    {
                        for (int i = 0; i < aLine.Count; i++)
                        {
                            if (i <= t1 && i >= t0)//中间
                            {
                                segInnerLine[aLine[i]] = 0;
                            }
                            else if (i <= t2 && i >= t1)//右一
                            {
                                segInnerLine[aLine[i]] = 1;
                            }
                            else if (i <= t0 && i >= t_1)//左一
                            {
                                segInnerLine[aLine[i]] = 2;
                            }
                        }
                        segInnerLine[new Point(9998, 9998)] = 3;
                        segInnerLine[new Point(9999, 9999)] = 4;
                    }
                    else
                    {
                        for (int i = 0; i < aLine.Count; i++)
                        {
                            if (i <= t1 && i >= t0)//中间
                            {
                                segInnerLine[aLine[i]] = 0;
                            }
                            else if (i <= t2 && i >= t1)//右一
                            {
                                segInnerLine[aLine[i]] = 1;
                            }
                            else if (i <= t0 && i >= t_1)//左一
                            {
                                segInnerLine[aLine[i]] = 2;
                            }
                            else if (i <= t3 && i >= t2)//右二
                            {
                                segInnerLine[aLine[i]] = 3;
                            }
                            else//左二
                            {
                                segInnerLine[aLine[i]] = 4;
                            }
                        }
                    }

                    segmentLine[segInnerLine] = "Bottom";
                }
                //TODO===计算List<double>存储5个线段距离中线的距离
                fiveD.Add(segInnerLine.Where(a => a.Value == 0).Select(a => a.Key).Average(a => { return GetPoint2LineDistance(a, A_, B_, C_); }));
                fiveD.Add(segInnerLine.Where(a => a.Value == 1).Select(a => a.Key).Average(a => { return GetPoint2LineDistance(a, A_, B_, C_); }));
                fiveD.Add(segInnerLine.Where(a => a.Value == 2).Select(a => a.Key).Average(a => { return GetPoint2LineDistance(a, A_, B_, C_); }));
                fiveD.Add(segInnerLine.Where(a => a.Value == 3).Select(a => a.Key).Average(a => { return GetPoint2LineDistance(a, A_, B_, C_); }));
                fiveD.Add(segInnerLine.Where(a => a.Value == 4).Select(a => a.Key).Average(a => { return GetPoint2LineDistance(a, A_, B_, C_); }));
            }
            return fiveD;
        }

        /// <summary>
        /// 计算点到线的距离
        /// </summary>
        /// <param name="p">点</param>
        /// <param name="A">AX</param>
        /// <param name="B">BY</param>
        /// <param name="C"></param>
        /// <returns></returns>
        private double GetPoint2LineDistance(Point p,double A,double B,double C)
        {
            double dis = 0;
            dis = Math.Abs(A * p.X + B * p.Y + C) / (Math.Sqrt(A * A + B * B));
            return dis;
        }
    }
}