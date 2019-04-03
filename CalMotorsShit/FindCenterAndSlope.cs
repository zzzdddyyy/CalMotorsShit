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
    public class FindCenterAndSlope
    {
        //字段
        public double RotatedAngle { get; set; }//记录旋转角
        public PointF CenterOfRobot { get; set; }//记录机器人法兰中心点坐标
        public PointF CenterOfImg { get; set; }//记录图像中心点坐标
        public PointF[] ImageCorner { get; set; }//记录图像角点信息
        public double AxisLong { get; set; }//记录长轴
        public double AxisShort { get; set; }//记录短轴
        public Image<Gray, byte> BinaryImage{get;set;}//记录二值化图像
        public Dictionary<Point, int> dividedCoutour;//记录四条边的点集
        public Dictionary<Dictionary<Point, int>,string> segmentLine;//记录单边分5组--临时用
        public List<double> motorShif = new List<double>();


        #region 全局变量
        private const int width = 5472;      //相机分辨率 2000W像素
        private const int height = 3648;
        private Size imageSize = new Size(width, height);//图像的大小
        private double pr=1.66;//  pixelRatio

        private Matrix<double> cameraMatrix = new Matrix<double>(3, 3);//相机内部参数
        private Matrix<double> distCoeffs = new Matrix<double>(5, 1);//畸变参数
        private Matrix<double> c2wFrontMatrix = new Matrix<double>(4, 4);//相机坐标系到Robot世界坐标系变换矩阵
        private Matrix<double> c2wRightMatrix = new Matrix<double>(4, 4);//相机坐标系到Robot世界坐标系变换矩阵

        private Matrix<float> mapx = new Matrix<float>(height, width); //x坐标对应的映射矩阵
        private Matrix<float> mapy = new Matrix<float>(height, width);
        private MCvTermCriteria criteria = new MCvTermCriteria(100, 1e-5);//求角点迭代的终止条件（精度）

        private Matrix<double> frontCameraTrans = new Matrix<double>(3, 3);
        private Matrix<double> rightCameraTrans = new Matrix<double>(3, 3);

        readonly Mat kernelClosing = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(-1, -1));//运算核
        #endregion

        /// <summary>
        /// 获取高架（右侧）相机矩阵和畸变矩阵，cameraID==0
        /// </summary>
        private void GetRightCamParams()
        {
            //填充相机矩阵
            cameraMatrix[0, 0] = 5059.12931834576;
            cameraMatrix[0, 1] = -0.692392244384381;
            cameraMatrix[0, 2] = 2750.92723044525;
            cameraMatrix[1, 0] = 0;
            cameraMatrix[1, 1] = 5057.53403912353;
            cameraMatrix[1, 2] = 1858.08491664551;
            cameraMatrix[2, 0] = 0;
            cameraMatrix[2, 1] = 0;
            cameraMatrix[2, 2] = 1;
            //填充畸变矩阵
            distCoeffs[0, 0] = -0.0627050596821338;//K1
            distCoeffs[1, 0] = 0.142587435311481;//K2
            distCoeffs[2, 0] = -0.000113201879186569;//P1
            distCoeffs[3, 0] = 0.000348821210979477;//P2
            distCoeffs[4, 0] = 0;//K3
            //填充坐标变换矩阵
            rightCameraTrans[0, 0] = -1.51127803e-02;
            rightCameraTrans[0, 1] = 6.43187885e-01;
            rightCameraTrans[0, 2] = -1.12344669e+03;
            rightCameraTrans[1, 0] = 6.44677412e-01;
            rightCameraTrans[1, 1] = 1.38359506e-02;
            rightCameraTrans[1, 2] = 8.27866536e+02;
            rightCameraTrans[2, 0] = -5.42101086e-20;
            rightCameraTrans[2, 1] = -6.77626358e-21;
            rightCameraTrans[2, 2] = 1.00000000e+00;
        }

        /// <summary>
        /// 获取前方相机矩阵和畸变矩阵，cameraID==1
        /// </summary>
        private void GetFrontCamParams()
        {
            //填充相机矩阵
            cameraMatrix[0, 0] = 5010.81789826726;
            cameraMatrix[0, 1] = 0.168076608984161;
            cameraMatrix[0, 2] = 2768.70959640510;
            cameraMatrix[1, 0] = 0;
            cameraMatrix[1, 1] = 5011.24572785725;
            cameraMatrix[1, 2] = 1806.34169717229;
            cameraMatrix[2, 0] = 0;
            cameraMatrix[2, 1] = 0;
            cameraMatrix[2, 2] = 1;
            //填充畸变矩阵,先径向再切向
            distCoeffs[0, 0] = -0.0625967952090845;//K1
            distCoeffs[1, 0] = 0.133984194777852;//K2
            distCoeffs[2, 0] = -0.000122713140590104;//P1
            distCoeffs[3, 0] = 0.00160031845139996;//P2
            distCoeffs[4, 0] = 0;//K3
            //填充坐标变换矩阵
            frontCameraTrans[0, 0] = -9.44537132e-05;
            frontCameraTrans[0, 1] = -6.26494111e-01;
            frontCameraTrans[0, 2] = 3.66581663e+03;
            frontCameraTrans[1, 0] = -6.29213379e-01;
            frontCameraTrans[1, 1] = 2.48650556e-03;
            frontCameraTrans[1, 2] = 1.62770209e+03;
            frontCameraTrans[2, 0] = -1.60936260e-20;
            frontCameraTrans[2, 1] = 5.42101086e-20;
            frontCameraTrans[2, 2] = 1.00000000e+00;
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
        private List<VectorOfPoint> GetContours(Bitmap img,int cameraID)
        {
            #region 灰度处理
            //灰度化
            Image<Gray, byte>  grayImg = new Image<Gray, byte>(img).PyrDown().PyrUp();
            Image<Gray, byte> resImg = grayImg.CopyBlank();
            Image<Gray, byte>  remapImg = grayImg.CopyBlank();//映射后图像
            //获取畸变参数
            if (cameraID == 0)
            {
                GetRightCamParams();
                resImg = GetROI(grayImg, new Rectangle(new Point(850, 0), new Size(4360 - 850, 3300)));
            }
            else
            {
                GetFrontCamParams();
                resImg = GetROI(grayImg, new Rectangle(new Point(700, 70), new Size(4300 - 700, 3500-70)));
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
                CvInvoke.FindContours(binaryImg, contours, dnc, RetrType.Ccomp, ChainApproxMethod.ChainApproxNone);//轮廓集合
                myContours.Clear();
                for (int k = 0; k < contours.Size; k++)
                {
                    double area = CvInvoke.ContourArea(contours[k]);//获取各连通域的面积 
                    if (area < 1000000)//根据面积作筛选(指定最小面积,最大面积2500000):
                    {
                        CvInvoke.FillConvexPoly(binaryImg, contours[k], new MCvScalar(0));
                    }
                    if (area > 1500000)//3000000
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
        public void GetProductParamters(Bitmap bitmap,int cameraID)
        {

            List<VectorOfPoint> imageContours = GetContours(bitmap, cameraID);
            VectorOfPoint productContour = imageContours.Max();
            Point[] pst = productContour.ToArray();//获取轮廓上所有的点集
            if (productContour!=null)
            {
                var minRect = CvInvoke.MinAreaRect(productContour); //最小外接矩形
                PointF[] pt = CvInvoke.BoxPoints(minRect);//最小外接矩形四个角点 
                dividedCoutour = new Dictionary<Point, int>();//储存四条分组的边的点集
     
                for (int i = 0; i < pt.Length; i++)
                {
                    Point p1 = new Point((int)pt[i].X, (int)pt[i].Y);
                    Point p2 = new Point((int)pt[(i + 1) % 4].X, (int)pt[(i + 1) % 4].Y);
                    if (p1.X<width/2&&p1.Y>height/2)//左
                    {
                        foreach (var item in pst)
                        {
                            if (item.X <= (p1.X >= p2.X ? p1.X : p2.X) + 4 && item.X >= (p1.X > p2.X ? p2.X : p1.X) - 4 && item.Y <= (p1.Y >= p2.Y ? p1.Y : p2.Y) + 8 && item.Y >= (p1.Y > p2.Y ? p2.Y : p1.Y) - 8)
                            {
                                dividedCoutour[item] =0;
                            }
                        }
                    }
                    else if (p1.X < width / 2 && p1.Y < height/2)//上
                    {
                        foreach (var item in pst)
                        {
                            if (item.X <= (p1.X >= p2.X ? p1.X : p2.X) + 8 && item.X >= (p1.X > p2.X ? p2.X : p1.X) - 8 && item.Y <= (p1.Y >= p2.Y ? p1.Y : p2.Y) +4 && item.Y >= (p1.Y > p2.Y ? p2.Y : p1.Y) - 4)
                            {
                                dividedCoutour[item] = 1;
                            }
                        }
                    }
                    else if (p1.X > width / 2 && p1.Y < height/2)//右
                    {
                        foreach (var item in pst)
                        {
                            if (item.X <= (p1.X >= p2.X ? p1.X : p2.X) + 4 && item.X >= (p1.X > p2.X ? p2.X : p1.X) - 4 && item.Y <= (p1.Y >= p2.Y ? p1.Y : p2.Y) + 8 && item.Y >= (p1.Y > p2.Y ? p2.Y : p1.Y) - 8)
                            {
                                dividedCoutour[item] = 2;
                            }
                        }
                    }
                    else//下
                    {
                        foreach (var item in pst)
                        {
                            if (item.X <= (p1.X >= p2.X ? p1.X : p2.X) + 8 && item.X >= (p1.X > p2.X ? p2.X : p1.X) - 8 && item.Y <= (p1.Y >= p2.Y ? p1.Y : p2.Y) + 4 && item.Y >= (p1.Y > p2.Y ? p2.Y : p1.Y) - 4)
                            {
                                dividedCoutour[item] = 3;
                            }
                        }
                    }
                }
                //foreach (var item in dividedCoutour)
                //{
                //    File.AppendAllText("ContourPoints.txt", "X=" + item.Key.X.ToString() +"\t\t"+ "Y=" + item.Key.Y.ToString() +"\t\t"+"Key="+item.Value.ToString()+ "\n\r");
                //}
                PointF po = minRect.Center;//最小外接矩形中心

                CenterOfImg = po;

                //长轴,短轴,倾角计算:
                //AxisLong =  Math.Sqrt(Math.Pow(pt[1].X - pt[0].X, 2) + Math.Pow(pt[1].Y - pt[0].Y, 2));
                AxisLong = minRect.Size.Width > minRect.Size.Height ? minRect.Size.Width : minRect.Size.Height;
                //AxisShort =  Math.Sqrt(Math.Pow(pt[2].X - pt[1].X, 2) + Math.Pow(pt[2].Y - pt[1].Y, 2));
                AxisShort = minRect.Size.Height <= minRect.Size.Width ? minRect.Size.Height : minRect.Size.Width; ;
                ImageCorner = pt;
                RotatedAngle = Math.Abs(minRect.Angle)>45?minRect.Angle+90:minRect.Angle;

                //验证一段分5组：
                List<double> up = new List<double>(); 
                List<double> bottom = new List<double>(); 
                List<double> left = new List<double>(); 
                List<double> right = new List<double>(); 
                left = GetFiveDistanceOnLine(RotatedAngle, new Point((int)po.X,(int)po.Y), AxisLong, AxisShort,dividedCoutour.Where(a => a.Value ==0).ToDictionary(a => a.Key, a => a.Value));
                if (cameraID==0)//R
                {
                    for (int i = 0; i < left.Count; i++)
                    {
                        if (left[i] > AxisShort * 2 / 3f)
                        {
                            left[i] = 0;
                        }
                    }
                }
                up = GetFiveDistanceOnLine(RotatedAngle, new Point((int)po.X,(int)po.Y), AxisLong, AxisShort,dividedCoutour.Where(a => a.Value ==1).ToDictionary(a => a.Key, a => a.Value));
                if (cameraID==1)
                {
                    for (int i = 0; i < up.Count; i++)
                    {
                        if (up[i] > AxisShort * 2 / 3f)
                        {
                            up[i] = 0;
                        }
                    }
                }
                right = GetFiveDistanceOnLine(RotatedAngle, new Point((int)po.X,(int)po.Y), AxisLong, AxisShort,dividedCoutour.Where(a => a.Value ==2).ToDictionary(a => a.Key, a => a.Value));
                if (cameraID==0)//R
                {
                    for (int i = 0; i < right.Count; i++)
                    {
                        if (right[i] > AxisShort*2 / 3f)
                        {
                            right[i] = 0;
                        }
                    }
                }
                bottom = GetFiveDistanceOnLine(RotatedAngle, new Point((int)po.X,(int)po.Y), AxisLong, AxisShort,dividedCoutour.Where(a => a.Value ==3).ToDictionary(a => a.Key, a => a.Value));
                if (cameraID==1)//F
                {
                    for (int i = 0; i < bottom.Count; i++)
                    {
                        if (bottom[i] > AxisShort * 2 / 3f)
                        {
                            bottom[i] = 0;
                        }
                    }
                }

                motorShif = left.Concat(up).Concat(right).Concat(bottom).ToList<double>();
                //Dictionary<Point, string> detailLinePoints = new Dictionary<Point, string>();
                //for (int i = 0; i < 4; i++)
                //{
                //    if (dividedCoutour.Where(xav=>xav.Value==i).Select(xav=>xav.Key).Average(xav=>xav.X)<width/2
                //        && dividedCoutour.Where(yav => yav.Value == i).Select(yav => yav.Key).Average(yav => yav.X) < height / 2)
                //    {
                //        dividedCoutour.Where(xav => xav.Value == i).
                //    }
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
                    
                    CenterOfRobot = new PointF((float.Parse((( rightCameraTrans* imgCenter)[0, 0]).ToString())), (float.Parse((( rightCameraTrans* imgCenter )[1, 0]).ToString())));
                }
                else
                {
                    CenterOfRobot = new PointF((float.Parse((( frontCameraTrans* imgCenter)[0, 0]).ToString())), (float.Parse((( frontCameraTrans* imgCenter )[1, 0]).ToString())));
                }
            }


        
        }

        /// <summary>
        /// 计算每条线上5个电机的移动距离
        /// </summary>
        /// <param name="angle">角度制</param>
        /// <param name="centerPoint"></param>
        /// <param name="AxisLong"></param>
        /// <param name="AxisShort"></param>
        /// <param name="linePoints"></param>
        /// <returns></returns>
        public List<double> GetFiveDistanceOnLine(double angle,Point centerPoint, double AxisLong,double AxisShort, Dictionary<Point,int> linePoints)
        {
            
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
        public double GetPoint2LineDistance(Point p,double A,double B,double C)
        {
            double dis = 0;
            dis = Math.Abs(A * p.X + B * p.Y + C) / (Math.Sqrt(A * A + B * B));
            return dis;
        }
    }
}
