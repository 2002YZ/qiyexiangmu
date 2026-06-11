using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanelHandingSystem
{



    //排版算法
    public class TypeSetAlgorithm
    {

        public static HTuple hv_ExpDefaultWinHandle;



        public static void CalBaseAdjustment(HTuple hv_PanelWidth, HTuple hv_PanelHeight, HTuple hv_PanelThickness,
            out HTuple hv_BaseAdjustVal)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_ZeroOffset = new HTuple(), hv_Rho_Epoxy = new HTuple();
            HTuple hv_Rho_Cu = new HTuple(), hv_Cu_Th = new HTuple();
            HTuple hv_Spring_Dist = new HTuple(), hv_Spring_Stiff = new HTuple();
            HTuple hv_G_Accel = new HTuple(), hv_W_m = new HTuple();
            HTuple hv_H_m = new HTuple(), hv_T_m = new HTuple(), hv_M_Epoxy = new HTuple();
            HTuple hv_Num_Layers = new HTuple(), hv_Vol_Face = new HTuple();
            HTuple hv_Vol_Edge1 = new HTuple(), hv_Vol_Edge2 = new HTuple();
            HTuple hv_Vol_Cu_Total = new HTuple(), hv_M_Cu = new HTuple();
            HTuple hv_M_Total = new HTuple(), hv_N_V = new HTuple();
            HTuple hv_F_Total = new HTuple(), hv_Disp = new HTuple();
            HTuple hv_Smax = new HTuple(), hv_Delta_Auto = new HTuple();
            // Initialize local and output iconic variables 
            hv_BaseAdjustVal = new HTuple();
            //设定自动化调节的起始位置（零点位）
            hv_ZeroOffset.Dispose();
            hv_ZeroOffset = 508.0;
            hv_Rho_Epoxy.Dispose();
            hv_Rho_Epoxy = 1800.0;
            hv_Rho_Cu.Dispose();
            hv_Rho_Cu = 8960.0;
            //铜厚 0.07mm 转换为 m
            hv_Cu_Th.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Cu_Th = 0.07 / 1000.0;
            }

            //弹簧参数
            hv_Spring_Dist.Dispose();
            hv_Spring_Dist = 120.0;
            hv_Spring_Stiff.Dispose();
            hv_Spring_Stiff = 0.34;
            hv_G_Accel.Dispose();
            hv_G_Accel = 9.81;

            //2. 单位转换
            hv_W_m.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_W_m = hv_PanelWidth / 1000.0;
            }
            hv_H_m.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_H_m = hv_PanelHeight / 1000.0;
            }
            hv_T_m.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_T_m = hv_PanelThickness / 1000.0;
            }

            //3. 计算 Epoxy 重量
            hv_M_Epoxy.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_M_Epoxy = ((hv_W_m * hv_H_m) * hv_T_m) * hv_Rho_Epoxy;
            }

            //4. 计算铜 重量
            //4.1 叠加块数
            hv_Num_Layers.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Num_Layers = ((hv_PanelThickness / 2.0)).TupleInt()
                    ;
            }


            //防止厚度小于2mm时计算为0，保底设为1
            //if (Num_Layers < 1)
            //Num_Layers := 1
            //endif

            //4.2 计算单层铜体积 (包含上下表面 + 四周侧边)
            //表面积体积
            hv_Vol_Face.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Vol_Face = ((hv_W_m * hv_H_m) * hv_Cu_Th) * 2.0;
            }
            //侧面积体积 (板宽侧 + 板长侧)
            hv_Vol_Edge1.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Vol_Edge1 = ((hv_W_m * hv_T_m) * hv_Cu_Th) * 2.0;
            }
            hv_Vol_Edge2.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Vol_Edge2 = ((hv_H_m * hv_T_m) * hv_Cu_Th) * 2.0;
            }

            hv_Vol_Cu_Total.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Vol_Cu_Total = ((hv_Vol_Face + hv_Vol_Edge1) + hv_Vol_Edge2) * hv_Num_Layers;
            }

            hv_M_Cu.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_M_Cu = hv_Vol_Cu_Total * hv_Rho_Cu;
            }

            //5. 总重量
            hv_M_Total.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_M_Total = hv_M_Epoxy + hv_M_Cu;
            }

            //6. 计算弹簧受力位移
            //6.1 计算占用的V座数量 (向下取整)
            hv_N_V.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_N_V = ((hv_PanelWidth / hv_Spring_Dist)).TupleInt()
                    ;
            }
            if ((int)(new HTuple(hv_N_V.TupleLess(1))) != 0)
            {
                hv_N_V.Dispose();
                hv_N_V = 1;
            }

            //6.2 重力
            hv_F_Total.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_F_Total = hv_M_Total * hv_G_Accel;
            }

            //6.3 位移量  = 总力 / (数量 * 刚度)
            hv_Disp.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Disp = hv_F_Total / (hv_N_V * hv_Spring_Stiff);
            }

            //**********************************Add
            hv_Smax.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Smax = ((hv_Disp.TupleRound()
                    )).TupleInt();
            }

            if ((int)(new HTuple(((hv_PanelHeight - hv_ZeroOffset)).TupleGreaterEqual(0))) != 0)
            {
                hv_Delta_Auto.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Delta_Auto = (hv_PanelHeight - hv_ZeroOffset) - hv_Smax;
                }
                hv_BaseAdjustVal.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_BaseAdjustVal = -hv_Delta_Auto;
                }
            }
            else
            {
                hv_Delta_Auto.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Delta_Auto = hv_ZeroOffset - (hv_PanelHeight - hv_Smax);
                }
                hv_BaseAdjustVal.Dispose();
                hv_BaseAdjustVal = new HTuple(hv_Delta_Auto);
            }


            hv_ZeroOffset.Dispose();
            hv_Rho_Epoxy.Dispose();
            hv_Rho_Cu.Dispose();
            hv_Cu_Th.Dispose();
            hv_Spring_Dist.Dispose();
            hv_Spring_Stiff.Dispose();
            hv_G_Accel.Dispose();
            hv_W_m.Dispose();
            hv_H_m.Dispose();
            hv_T_m.Dispose();
            hv_M_Epoxy.Dispose();
            hv_Num_Layers.Dispose();
            hv_Vol_Face.Dispose();
            hv_Vol_Edge1.Dispose();
            hv_Vol_Edge2.Dispose();
            hv_Vol_Cu_Total.Dispose();
            hv_M_Cu.Dispose();
            hv_M_Total.Dispose();
            hv_N_V.Dispose();
            hv_F_Total.Dispose();
            hv_Disp.Dispose();
            hv_Smax.Dispose();
            hv_Delta_Auto.Dispose();

            return;
        }







        public static void CalBestPeiduWidthTu(HTuple hv_TotalWidth, HTuple hv_TotalHeight, HTuple hv_PanelWidth,
    HTuple hv_PanelHeight, HTuple hv_FullThres, HTuple hv_Inteval, out HTuple hv_PeiDuWidth_out,
    out HTuple hv_PeiDuHeight, out HTuple hv_PanelCount, out HTuple hv_PeiDuExist)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_Interval = new HTuple(), hv_countPanel1 = new HTuple();
            HTuple hv_countPanel = new HTuple(), hv_WidthMin = new HTuple();
            HTuple hv_WidthMax = new HTuple(), hv_ZMax = new HTuple();
            HTuple hv_ZMin = new HTuple(), hv_PeiDuYuliang = new HTuple();
            HTuple hv_Tolerance = new HTuple(), hv_StepDownWidth = new HTuple();
            HTuple hv_FullThres_COPY_INP_TMP = new HTuple(hv_FullThres);
            HTuple hv_PanelHeight_COPY_INP_TMP = new HTuple(hv_PanelHeight);
            HTuple hv_PanelWidth_COPY_INP_TMP = new HTuple(hv_PanelWidth);
            HTuple hv_TotalHeight_COPY_INP_TMP = new HTuple(hv_TotalHeight);
            HTuple hv_TotalWidth_COPY_INP_TMP = new HTuple(hv_TotalWidth);

            // Initialize local and output iconic variables 
            hv_PeiDuWidth_out = new HTuple();
            hv_PeiDuHeight = new HTuple();
            hv_PanelCount = new HTuple();
            hv_PeiDuExist = new HTuple();
            //根据板宽，计算最佳的陪镀板宽度
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_PanelWidth = hv_PanelWidth_COPY_INP_TMP.TupleReal()
                        ;
                    hv_PanelWidth_COPY_INP_TMP.Dispose();
                    hv_PanelWidth_COPY_INP_TMP = ExpTmpLocalVar_PanelWidth;
                }
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_PanelHeight = hv_PanelHeight_COPY_INP_TMP.TupleReal()
                        ;
                    hv_PanelHeight_COPY_INP_TMP.Dispose();
                    hv_PanelHeight_COPY_INP_TMP = ExpTmpLocalVar_PanelHeight;
                }
            }

            //占满留边阈值，理论值等于一个夹子的宽度，两边除二
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_FullThres = hv_FullThres_COPY_INP_TMP.TupleReal()
                        ;
                    hv_FullThres_COPY_INP_TMP.Dispose();
                    hv_FullThres_COPY_INP_TMP = ExpTmpLocalVar_FullThres;
                }
            }

            //区域总长宽，单位mm
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_TotalWidth = hv_TotalWidth_COPY_INP_TMP.TupleReal()
                        ;
                    hv_TotalWidth_COPY_INP_TMP.Dispose();
                    hv_TotalWidth_COPY_INP_TMP = ExpTmpLocalVar_TotalWidth;
                }
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_TotalHeight = hv_TotalHeight_COPY_INP_TMP.TupleReal()
                        ;
                    hv_TotalHeight_COPY_INP_TMP.Dispose();
                    hv_TotalHeight_COPY_INP_TMP = ExpTmpLocalVar_TotalHeight;
                }
            }

            //板子之间的间隙，单位mm
            hv_Interval.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Interval = hv_Inteval.TupleReal()
                    ;
            }

            //******************************************************************

            //计算宽度方向能放几块板子
            hv_countPanel1.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_countPanel1 = hv_TotalWidth_COPY_INP_TMP / hv_PanelWidth_COPY_INP_TMP;
            }
            hv_countPanel.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_countPanel = hv_countPanel1.TupleInt()
                    ;
            }


            //根据表格中的主板片数量范围，重新计算主板数量
            if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(400.0))).TupleAnd(
                new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(630.0)))) != 0)
            {

                if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(596.0))).TupleAnd(
                    new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(630.0)))) != 0)
                {
                    //596-630mm -> 5片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 5;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    559.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    595.0)))) != 0)
                {
                    //559-595mm -> 5片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 5;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    529.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    558.0)))) != 0)
                {
                    hv_countPanel.Dispose();
                    hv_countPanel = 5;
                    //559-595mm -> 5片主板
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    510.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    528.0)))) != 0)
                {
                    //510-528mm -> 6片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 6;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    479.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    509.0)))) != 0)
                {
                    //479-509mm -> 6片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 6;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    453.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    478.0)))) != 0)
                {
                    //453-478mm -> 6片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 6;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    445.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    452.0)))) != 0)
                {
                    //445-452mm -> 7片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 7;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    419.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    444.0)))) != 0)
                {
                    //419-444mm -> 7片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 7;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    400.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    418.0)))) != 0)
                {
                    //400-418mm -> 7片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 7;
                }
            }
            //计算陪镀板宽度，输出为0时，表示没有陪镀板
            hv_PeiDuWidth_out.Dispose();
            hv_PeiDuWidth_out = 0.0;




            if ((int)(new HTuple(hv_countPanel.TupleEqual(7))) != 0)
            {
                //7片主板的分段规则
                if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(400.0))).TupleAnd(
                    new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(418.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 212.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    419.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    444.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 118.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    445.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    452.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 90.0;
                }
            }
            else if ((int)(new HTuple(hv_countPanel.TupleEqual(6))) != 0)
            {
                //6片主板的分段规则
                if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(453.0))).TupleAnd(
                    new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(478.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 241.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    479.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    509.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 148.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    510.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    528.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 90.0;
                }

            }
            else if ((int)(new HTuple(hv_countPanel.TupleEqual(5))) != 0)
            {
                //5片主板的分段规则
                if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(529.0))).TupleAnd(
                    new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(558.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 245.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    559.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    595.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 187.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    596.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    630.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 97.0;
                }



            }
            else
            {
                //对于不在表格范围内的板宽，使用通用计算逻辑
                hv_WidthMin.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_WidthMin = (((hv_TotalWidth_COPY_INP_TMP / (hv_countPanel + 1))).TupleInt()
                        ) + 1;
                }
                hv_WidthMax.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_WidthMax = ((hv_TotalWidth_COPY_INP_TMP / hv_countPanel)).TupleInt()
                        ;
                }

                if ((int)(new HTuple(hv_WidthMin.TupleLess(400))) != 0)
                {
                    hv_WidthMin.Dispose();
                    hv_WidthMin = 400;
                }

                if ((int)(new HTuple(hv_WidthMax.TupleGreater(630))) != 0)
                {
                    hv_WidthMax.Dispose();
                    hv_WidthMax = 630;
                }

                hv_ZMax.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_ZMax = (((hv_TotalWidth_COPY_INP_TMP - (hv_WidthMin * hv_countPanel)) / 2)).TupleInt()
                        ;
                }
                hv_ZMin.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_ZMin = (((hv_TotalWidth_COPY_INP_TMP - (hv_WidthMax * hv_countPanel)) / 2)).TupleInt()
                        ;
                }

                if ((int)(new HTuple(hv_ZMax.TupleGreater(hv_FullThres_COPY_INP_TMP))) != 0)
                {
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_ZMax = hv_ZMax - (hv_FullThres_COPY_INP_TMP / 2);
                            hv_ZMax.Dispose();
                            hv_ZMax = ExpTmpLocalVar_ZMax;
                        }
                    }
                }

                //计算当前板宽对应的剩余空间
                hv_PeiDuYuliang.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_PeiDuYuliang = hv_TotalWidth_COPY_INP_TMP - (hv_countPanel * hv_PanelWidth_COPY_INP_TMP);
                }
                hv_Tolerance.Dispose();
                hv_Tolerance = 15.0;

                if ((int)(new HTuple(hv_PeiDuYuliang.TupleLess(hv_FullThres_COPY_INP_TMP))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 0.0;
                }
                else if ((int)(new HTuple(((hv_PeiDuYuliang + hv_Tolerance)).TupleLess(
                    hv_ZMax * 2))) != 0)
                {
                    hv_StepDownWidth.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_StepDownWidth = hv_ZMax - (hv_FullThres_COPY_INP_TMP / 2);
                    }
                    if ((int)((new HTuple(hv_StepDownWidth.TupleGreaterEqual(hv_FullThres_COPY_INP_TMP / 2))).TupleAnd(
                        new HTuple(hv_StepDownWidth.TupleLessEqual((hv_PeiDuYuliang + hv_Tolerance) / 2)))) != 0)
                    {
                        hv_PeiDuWidth_out.Dispose();
                        hv_PeiDuWidth_out = new HTuple(hv_StepDownWidth);
                    }
                    else
                    {
                        hv_PeiDuWidth_out.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_PeiDuWidth_out = hv_FullThres_COPY_INP_TMP / 2;
                        }
                    }
                }
                else
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = new HTuple(hv_ZMax);
                }
            }

            //确保陪镀板宽度不超过最大值限制
            if ((int)(new HTuple(hv_PeiDuWidth_out.TupleGreater(300))) != 0)
            {
                hv_PeiDuWidth_out.Dispose();
                hv_PeiDuWidth_out = 300.0;
            }

            //计算陪镀板高度
            if ((int)(new HTuple(hv_PeiDuWidth_out.TupleGreater(0))) != 0)
            {
                if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(450))).TupleAnd(
                    new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLessEqual(700)))) != 0)
                {
                    if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(450))).TupleAnd(
                        new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLess(500)))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 450.0;
                    }
                    else if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(
                        500))).TupleAnd(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLess(550)))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 500.0;
                    }
                    else if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(
                        550))).TupleAnd(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLess(600)))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 550.0;
                    }
                    else if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(
                        600))).TupleAnd(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLess(650)))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 600.0;
                    }
                    else if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(
                        650))).TupleAnd(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLess(700)))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 650.0;
                    }
                    else if ((int)(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(
                        700))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 650.0;
                    }
                }
                else if ((int)(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreater(
                    700))) != 0)
                {

                    hv_PeiDuHeight.Dispose();
                    hv_PeiDuHeight = 700.0;
                }
                else
                {
                    hv_PeiDuHeight.Dispose();
                    hv_PeiDuHeight = 450.0;
                }
            }
            else
            {
                hv_PeiDuHeight.Dispose();
                hv_PeiDuHeight = 0.0;
            }


            //计算是否存在陪镀板
            if ((int)(new HTuple(hv_PeiDuWidth_out.TupleGreater(0.0))) != 0)
            {
                hv_PeiDuExist.Dispose();
                hv_PeiDuExist = 1;
            }
            else
            {
                hv_PeiDuExist.Dispose();
                hv_PeiDuExist = 0;
            }

            //输出主板数量
            hv_PanelCount.Dispose();
            hv_PanelCount = new HTuple(hv_countPanel);


            hv_FullThres_COPY_INP_TMP.Dispose();
            hv_PanelHeight_COPY_INP_TMP.Dispose();
            hv_PanelWidth_COPY_INP_TMP.Dispose();
            hv_TotalHeight_COPY_INP_TMP.Dispose();
            hv_TotalWidth_COPY_INP_TMP.Dispose();
            hv_Interval.Dispose();
            hv_countPanel1.Dispose();
            hv_countPanel.Dispose();
            hv_WidthMin.Dispose();
            hv_WidthMax.Dispose();
            hv_ZMax.Dispose();
            hv_ZMin.Dispose();
            hv_PeiDuYuliang.Dispose();
            hv_Tolerance.Dispose();
            hv_StepDownWidth.Dispose();

            return;
        }


        public static void CalBestPeiduWidthBan(HTuple hv_TotalWidth, HTuple hv_TotalHeight, HTuple hv_PanelWidth,
    HTuple hv_PanelHeight, HTuple hv_FullThres, HTuple hv_Inteval, out HTuple hv_PeiDuWidth_out,
    out HTuple hv_PeiDuHeight, out HTuple hv_PanelCount, out HTuple hv_PeiDuExist)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_Interval = new HTuple(), hv_countPanel1 = new HTuple();
            HTuple hv_countPanel = new HTuple(), hv_WidthMin = new HTuple();
            HTuple hv_WidthMax = new HTuple(), hv_ZMax = new HTuple();
            HTuple hv_ZMin = new HTuple(), hv_PeiDuYuliang = new HTuple();
            HTuple hv_Tolerance = new HTuple(), hv_StepDownWidth = new HTuple();
            HTuple hv_FullThres_COPY_INP_TMP = new HTuple(hv_FullThres);
            HTuple hv_PanelHeight_COPY_INP_TMP = new HTuple(hv_PanelHeight);
            HTuple hv_PanelWidth_COPY_INP_TMP = new HTuple(hv_PanelWidth);
            HTuple hv_TotalHeight_COPY_INP_TMP = new HTuple(hv_TotalHeight);
            HTuple hv_TotalWidth_COPY_INP_TMP = new HTuple(hv_TotalWidth);

            // Initialize local and output iconic variables 
            hv_PeiDuWidth_out = new HTuple();
            hv_PeiDuHeight = new HTuple();
            hv_PanelCount = new HTuple();
            hv_PeiDuExist = new HTuple();
            //根据板宽，计算最佳的陪镀板宽度
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_PanelWidth = hv_PanelWidth_COPY_INP_TMP.TupleReal()
                        ;
                    hv_PanelWidth_COPY_INP_TMP.Dispose();
                    hv_PanelWidth_COPY_INP_TMP = ExpTmpLocalVar_PanelWidth;
                }
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_PanelHeight = hv_PanelHeight_COPY_INP_TMP.TupleReal()
                        ;
                    hv_PanelHeight_COPY_INP_TMP.Dispose();
                    hv_PanelHeight_COPY_INP_TMP = ExpTmpLocalVar_PanelHeight;
                }
            }

            //占满留边阈值，理论值等于一个夹子的宽度，两边除二
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_FullThres = hv_FullThres_COPY_INP_TMP.TupleReal()
                        ;
                    hv_FullThres_COPY_INP_TMP.Dispose();
                    hv_FullThres_COPY_INP_TMP = ExpTmpLocalVar_FullThres;
                }
            }

            //区域总长宽，单位mm
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_TotalWidth = hv_TotalWidth_COPY_INP_TMP.TupleReal()
                        ;
                    hv_TotalWidth_COPY_INP_TMP.Dispose();
                    hv_TotalWidth_COPY_INP_TMP = ExpTmpLocalVar_TotalWidth;
                }
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_TotalHeight = hv_TotalHeight_COPY_INP_TMP.TupleReal()
                        ;
                    hv_TotalHeight_COPY_INP_TMP.Dispose();
                    hv_TotalHeight_COPY_INP_TMP = ExpTmpLocalVar_TotalHeight;
                }
            }

            //板子之间的间隙，单位mm
            hv_Interval.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_Interval = hv_Inteval.TupleReal()
                    ;
            }

            //******************************************************************

            //计算宽度方向能放几块板子
            hv_countPanel1.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_countPanel1 = hv_TotalWidth_COPY_INP_TMP / hv_PanelWidth_COPY_INP_TMP;
            }
            hv_countPanel.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_countPanel = hv_countPanel1.TupleInt()
                    ;
            }


            //根据表格中的主板片数量范围，重新计算主板数量
            if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(400.0))).TupleAnd(
                new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(630.0)))) != 0)
            {

                if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(596.0))).TupleAnd(
                    new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(630.0)))) != 0)
                {
                    //596-630mm -> 5片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 5;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    559.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    595.0)))) != 0)
                {
                    //559-595mm -> 5片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 5;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    529.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    558.0)))) != 0)
                {
                    hv_countPanel.Dispose();
                    hv_countPanel = 6;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    510.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    528.0)))) != 0)
                {
                    //510-528mm -> 6片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 6;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    479.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    509.0)))) != 0)
                {
                    //479-509mm -> 6片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 6;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    453.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    478.0)))) != 0)
                {
                    //453-478mm -> 6片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 7;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    445.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    452.0)))) != 0)
                {
                    //445-452mm -> 7片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 7;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    419.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    444.0)))) != 0)
                {
                    //419-444mm -> 7片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 7;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    400.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    418.0)))) != 0)
                {
                    //400-418mm -> 8片主板
                    hv_countPanel.Dispose();
                    hv_countPanel = 8;
                }
            }
            //计算陪镀板宽度，输出为0时，表示没有陪镀板
            hv_PeiDuWidth_out.Dispose();
            hv_PeiDuWidth_out = 0.0;




            if ((int)(new HTuple(hv_countPanel.TupleEqual(8))) != 0)
            {
                //8片主板的分段规则
                if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(400.0))).TupleAnd(
                    new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(418.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 0;
                }
            }
            else if ((int)(new HTuple(hv_countPanel.TupleEqual(7))) != 0)
            {
                //7片主板的分段规则
                if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(419.0))).TupleAnd(
                    new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(444.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 118.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    445.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    452.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 90.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    453.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    478.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 0;
                }
            }
            else if ((int)(new HTuple(hv_countPanel.TupleEqual(6))) != 0)
            {
                //6片主板的分段规则
                if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(479.0))).TupleAnd(
                    new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(509.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 148.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    510.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    528.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 90.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    529.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    558.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 0.0;
                }

            }
            else if ((int)(new HTuple(hv_countPanel.TupleEqual(5))) != 0)
            {
                //5片主板的分段规则
                if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(559.0))).TupleAnd(
                    new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(595.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 187.0;
                }
                else if ((int)((new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleGreaterEqual(
                    596.0))).TupleAnd(new HTuple(hv_PanelWidth_COPY_INP_TMP.TupleLessEqual(
                    630.0)))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 97.0;
                }

            }
            else
            {
                //对于不在表格范围内的板宽，使用通用计算逻辑
                hv_WidthMin.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_WidthMin = (((hv_TotalWidth_COPY_INP_TMP / (hv_countPanel + 1))).TupleInt()
                        ) + 1;
                }
                hv_WidthMax.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_WidthMax = ((hv_TotalWidth_COPY_INP_TMP / hv_countPanel)).TupleInt()
                        ;
                }

                if ((int)(new HTuple(hv_WidthMin.TupleLess(400))) != 0)
                {
                    hv_WidthMin.Dispose();
                    hv_WidthMin = 400;
                }

                if ((int)(new HTuple(hv_WidthMax.TupleGreater(630))) != 0)
                {
                    hv_WidthMax.Dispose();
                    hv_WidthMax = 630;
                }

                hv_ZMax.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_ZMax = (((hv_TotalWidth_COPY_INP_TMP - (hv_WidthMin * hv_countPanel)) / 2)).TupleInt()
                        ;
                }
                hv_ZMin.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_ZMin = (((hv_TotalWidth_COPY_INP_TMP - (hv_WidthMax * hv_countPanel)) / 2)).TupleInt()
                        ;
                }

                if ((int)(new HTuple(hv_ZMax.TupleGreater(hv_FullThres_COPY_INP_TMP))) != 0)
                {
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_ZMax = hv_ZMax - (hv_FullThres_COPY_INP_TMP / 2);
                            hv_ZMax.Dispose();
                            hv_ZMax = ExpTmpLocalVar_ZMax;
                        }
                    }
                }

                //计算当前板宽对应的剩余空间
                hv_PeiDuYuliang.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_PeiDuYuliang = hv_TotalWidth_COPY_INP_TMP - (hv_countPanel * hv_PanelWidth_COPY_INP_TMP);
                }
                hv_Tolerance.Dispose();
                hv_Tolerance = 15.0;

                if ((int)(new HTuple(hv_PeiDuYuliang.TupleLess(hv_FullThres_COPY_INP_TMP))) != 0)
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = 0.0;
                }
                else if ((int)(new HTuple(((hv_PeiDuYuliang + hv_Tolerance)).TupleLess(
                    hv_ZMax * 2))) != 0)
                {
                    hv_StepDownWidth.Dispose();
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_StepDownWidth = hv_ZMax - (hv_FullThres_COPY_INP_TMP / 2);
                    }
                    if ((int)((new HTuple(hv_StepDownWidth.TupleGreaterEqual(hv_FullThres_COPY_INP_TMP / 2))).TupleAnd(
                        new HTuple(hv_StepDownWidth.TupleLessEqual((hv_PeiDuYuliang + hv_Tolerance) / 2)))) != 0)
                    {
                        hv_PeiDuWidth_out.Dispose();
                        hv_PeiDuWidth_out = new HTuple(hv_StepDownWidth);
                    }
                    else
                    {
                        hv_PeiDuWidth_out.Dispose();
                        using (HDevDisposeHelper dh = new HDevDisposeHelper())
                        {
                            hv_PeiDuWidth_out = hv_FullThres_COPY_INP_TMP / 2;
                        }
                    }
                }
                else
                {
                    hv_PeiDuWidth_out.Dispose();
                    hv_PeiDuWidth_out = new HTuple(hv_ZMax);
                }
            }

            //确保陪镀板宽度不超过最大值限制
            if ((int)(new HTuple(hv_PeiDuWidth_out.TupleGreater(300))) != 0)
            {
                hv_PeiDuWidth_out.Dispose();
                hv_PeiDuWidth_out = 300.0;
            }

            //计算陪镀板高度
            if ((int)(new HTuple(hv_PeiDuWidth_out.TupleGreater(0))) != 0)
            {
                if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(450))).TupleAnd(
                    new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLessEqual(700)))) != 0)
                {
                    if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(450))).TupleAnd(
                        new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLess(500)))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 450.0;
                    }
                    else if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(
                        500))).TupleAnd(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLess(550)))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 500.0;
                    }
                    else if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(
                        550))).TupleAnd(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLess(600)))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 550.0;
                    }
                    else if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(
                        600))).TupleAnd(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLess(650)))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 600.0;
                    }
                    else if ((int)((new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(
                        650))).TupleAnd(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleLess(700)))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 650.0;
                    }
                    else if ((int)(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreaterEqual(
                        700))) != 0)
                    {
                        hv_PeiDuHeight.Dispose();
                        hv_PeiDuHeight = 650.0;
                    }
                }
                else if ((int)(new HTuple(hv_PanelHeight_COPY_INP_TMP.TupleGreater(
                    700))) != 0)
                {

                    hv_PeiDuHeight.Dispose();
                    hv_PeiDuHeight = 700.0;
                }
                else
                {
                    hv_PeiDuHeight.Dispose();
                    hv_PeiDuHeight = 450.0;
                }
            }
            else
            {
                hv_PeiDuHeight.Dispose();
                hv_PeiDuHeight = 0.0;
            }


            //计算是否存在陪镀板
            if ((int)(new HTuple(hv_PeiDuWidth_out.TupleGreater(0.0))) != 0)
            {
                hv_PeiDuExist.Dispose();
                hv_PeiDuExist = 1;
            }
            else
            {
                hv_PeiDuExist.Dispose();
                hv_PeiDuExist = 0;
            }

            //输出主板数量
            hv_PanelCount.Dispose();
            hv_PanelCount = new HTuple(hv_countPanel);


            hv_FullThres_COPY_INP_TMP.Dispose();
            hv_PanelHeight_COPY_INP_TMP.Dispose();
            hv_PanelWidth_COPY_INP_TMP.Dispose();
            hv_TotalHeight_COPY_INP_TMP.Dispose();
            hv_TotalWidth_COPY_INP_TMP.Dispose();
            hv_Interval.Dispose();
            hv_countPanel1.Dispose();
            hv_countPanel.Dispose();
            hv_WidthMin.Dispose();
            hv_WidthMax.Dispose();
            hv_ZMax.Dispose();
            hv_ZMin.Dispose();
            hv_PeiDuYuliang.Dispose();
            hv_Tolerance.Dispose();
            hv_StepDownWidth.Dispose();

            return;
        }



        public static void LayoutCalTu(HTuple hv_TotalWidth, HTuple hv_TotalHeight, HTuple hv_PanelWidth,
HTuple hv_PanelHeight, HTuple hv_PanelCount, HTuple hv_PeiDuWidth, HTuple hv_FullThres,
HTuple hv_Interval, HTuple hv_WindowHandle, HTuple hv_PeiDuHeight, HTuple hv_BaseAdjustVal,
HTuple hv_PanelThickness, HTuple hv_PeiDuExist, out HTuple hv_PlatePosX, out HTuple hv_PlatePosY,
out HTuple hv_PeiDuPosX, out HTuple hv_PeiDuPosY)
        {



            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_Image, ho_ImageBackGround, ho_RegionBanZiAll;
            HObject ho_RegionPeiDu1 = null, ho_RegionPeiDu2 = null, ho_RegionBanZi;
            HObject ho_RegionTemp = null, ho_ObjectSelected = null;

            // Local control variables 

            HTuple hv_countPanel = new HTuple(), hv_WidthPanelAll = new HTuple();
            HTuple hv_NeedPeiDuBan = new HTuple(), hv_RowAll1 = new HTuple();
            HTuple hv_RowAll2 = new HTuple(), hv_ColAll1 = new HTuple();
            HTuple hv_ColAll2 = new HTuple(), hv_Index = new HTuple();
            HTuple hv_Row1 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Col1 = new HTuple(), hv_Col2 = new HTuple();
            HTuple hv_CountBanZi = new HTuple(), hv_PlatePosXStr = new HTuple();
            HTuple hv_PlatePosYStr = new HTuple(), hv_Ind = new HTuple();
            HTuple hv_Roww = new HTuple(), hv_Coll = new HTuple();
            HTuple hv_CollStr = new HTuple(), hv_RowwStr = new HTuple();
            HTuple hv_PeiDuPosXStr = new HTuple(), hv_PeiDuPosYStr = new HTuple();
            HTuple hv_PeiDuExistStr = new HTuple(), hv_FreeWidthAll = new HTuple();
            HTuple hv_str1 = new HTuple(), hv_str = new HTuple();
            HTuple hv_FullThres_COPY_INP_TMP = new HTuple(hv_FullThres);
            HTuple hv_Interval_COPY_INP_TMP = new HTuple(hv_Interval);
            HTuple hv_PanelHeight_COPY_INP_TMP = new HTuple(hv_PanelHeight);
            HTuple hv_PanelWidth_COPY_INP_TMP = new HTuple(hv_PanelWidth);
            HTuple hv_PeiDuWidth_COPY_INP_TMP = new HTuple(hv_PeiDuWidth);
            HTuple hv_TotalHeight_COPY_INP_TMP = new HTuple(hv_TotalHeight);
            HTuple hv_TotalWidth_COPY_INP_TMP = new HTuple(hv_TotalWidth);

            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Image);
            HOperatorSet.GenEmptyObj(out ho_ImageBackGround);
            HOperatorSet.GenEmptyObj(out ho_RegionBanZiAll);
            HOperatorSet.GenEmptyObj(out ho_RegionPeiDu1);
            HOperatorSet.GenEmptyObj(out ho_RegionPeiDu2);
            HOperatorSet.GenEmptyObj(out ho_RegionBanZi);
            HOperatorSet.GenEmptyObj(out ho_RegionTemp);
            HOperatorSet.GenEmptyObj(out ho_ObjectSelected);
            hv_PlatePosX = new HTuple();
            hv_PlatePosY = new HTuple();
            hv_PeiDuPosX = new HTuple();
            hv_PeiDuPosY = new HTuple();

            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_PanelWidth = hv_PanelWidth_COPY_INP_TMP.TupleReal()
                        ;
                    hv_PanelWidth_COPY_INP_TMP.Dispose();
                    hv_PanelWidth_COPY_INP_TMP = ExpTmpLocalVar_PanelWidth;
                }
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_PanelHeight = hv_PanelHeight_COPY_INP_TMP.TupleReal()
                        ;
                    hv_PanelHeight_COPY_INP_TMP.Dispose();
                    hv_PanelHeight_COPY_INP_TMP = ExpTmpLocalVar_PanelHeight;
                }
            }

            //陪镀板宽度
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_PeiDuWidth = hv_PeiDuWidth_COPY_INP_TMP.TupleReal()
                        ;
                    hv_PeiDuWidth_COPY_INP_TMP.Dispose();
                    hv_PeiDuWidth_COPY_INP_TMP = ExpTmpLocalVar_PeiDuWidth;
                }
            }

            //占满留边阈值，理论值等于一个夹子的宽度 ，两边除二
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_FullThres = hv_FullThres_COPY_INP_TMP.TupleReal()
                        ;
                    hv_FullThres_COPY_INP_TMP.Dispose();
                    hv_FullThres_COPY_INP_TMP = ExpTmpLocalVar_FullThres;
                }
            }

            //区域总长宽，单位mm
            //宽度设置要考虑陪镀板余量
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_TotalWidth = hv_TotalWidth_COPY_INP_TMP.TupleReal()
                        ;
                    hv_TotalWidth_COPY_INP_TMP.Dispose();
                    hv_TotalWidth_COPY_INP_TMP = ExpTmpLocalVar_TotalWidth;
                }
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_TotalHeight = hv_TotalHeight_COPY_INP_TMP.TupleReal()
                        ;
                    hv_TotalHeight_COPY_INP_TMP.Dispose();
                    hv_TotalHeight_COPY_INP_TMP = ExpTmpLocalVar_TotalHeight;
                }
            }

            //板子之间的间隙，单位mm
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_Interval = hv_Interval_COPY_INP_TMP.TupleReal()
                        ;
                    hv_Interval_COPY_INP_TMP.Dispose();
                    hv_Interval_COPY_INP_TMP = ExpTmpLocalVar_Interval;
                }
            }



            //******************************
            //20250112,根据输入的板子个数，陪镀板是否存在和陪镀板宽度，计算排版


            //******************************************************************

            ho_Image.Dispose();
            HOperatorSet.GenImageConst(out ho_Image, "byte", hv_TotalWidth_COPY_INP_TMP,
                hv_TotalHeight_COPY_INP_TMP);
            ho_ImageBackGround.Dispose();
            HOperatorSet.PaintRegion(ho_Image, ho_Image, out ho_ImageBackGround, 128, "fill");



            //直接使用输入值

            hv_countPanel.Dispose();
            hv_countPanel = new HTuple(hv_PanelCount);
            //计算countPanel个板子总长
            hv_WidthPanelAll.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_WidthPanelAll = hv_countPanel * hv_PanelWidth_COPY_INP_TMP;
            }

            //检查是否需要陪镀板

            hv_NeedPeiDuBan.Dispose();
            hv_NeedPeiDuBan = 0;
            if ((int)(new HTuple(hv_PeiDuWidth_COPY_INP_TMP.TupleGreater(0))) != 0)
            {
                //满足占满条件
                hv_NeedPeiDuBan.Dispose();
                hv_NeedPeiDuBan = 1;

            }





            //计算板子整体区域，不含陪镀板
            hv_WidthPanelAll.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_WidthPanelAll = (hv_countPanel * hv_PanelWidth_COPY_INP_TMP) + ((hv_countPanel - 1) * hv_Interval_COPY_INP_TMP);
            }
            //*生成板子整体区域,高度方向居中
            hv_RowAll1.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_RowAll1 = (hv_TotalHeight_COPY_INP_TMP / 2) - (hv_PanelHeight_COPY_INP_TMP / 2);
            }
            hv_RowAll2.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_RowAll2 = (hv_TotalHeight_COPY_INP_TMP / 2) + (hv_PanelHeight_COPY_INP_TMP / 2);
            }
            hv_ColAll1.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_ColAll1 = (hv_TotalWidth_COPY_INP_TMP / 2) - (hv_WidthPanelAll / 2);
            }
            hv_ColAll2.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_ColAll2 = (hv_TotalWidth_COPY_INP_TMP / 2) + (hv_WidthPanelAll / 2);
            }
            ho_RegionBanZiAll.Dispose();
            HOperatorSet.GenRectangle1(out ho_RegionBanZiAll, hv_RowAll1, hv_ColAll1, hv_RowAll2,
                hv_ColAll2);

            //生成陪镀板
            if ((int)(new HTuple(hv_NeedPeiDuBan.TupleEqual(1))) != 0)
            {

                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_RegionPeiDu1.Dispose();
                    HOperatorSet.GenRectangle1(out ho_RegionPeiDu1, hv_RowAll1, (hv_ColAll1 - hv_PeiDuWidth_COPY_INP_TMP) - hv_Interval_COPY_INP_TMP,
                        hv_RowAll2, hv_ColAll1 - hv_Interval_COPY_INP_TMP);
                }
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_RegionPeiDu2.Dispose();
                    HOperatorSet.GenRectangle1(out ho_RegionPeiDu2, hv_RowAll1, hv_ColAll2 + hv_Interval_COPY_INP_TMP,
                        hv_RowAll2, (hv_ColAll2 + hv_PeiDuWidth_COPY_INP_TMP) + hv_Interval_COPY_INP_TMP);
                }


            }



            //计算每个板子的区域
            ho_RegionBanZi.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_RegionBanZi);
            HTuple end_val72 = hv_countPanel;
            HTuple step_val72 = 1;
            for (hv_Index = 1; hv_Index.Continue(end_val72, step_val72); hv_Index = hv_Index.TupleAdd(step_val72))
            {

                hv_Row1.Dispose();
                hv_Row1 = new HTuple(hv_RowAll1);
                hv_Row2.Dispose();
                hv_Row2 = new HTuple(hv_RowAll2);
                hv_Col1.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Col1 = (hv_ColAll1 + ((hv_Index - 1) * hv_PanelWidth_COPY_INP_TMP)) + ((hv_Index - 1) * hv_Interval_COPY_INP_TMP);
                }
                hv_Col2.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Col2 = hv_Col1 + hv_PanelWidth_COPY_INP_TMP;
                }

                ho_RegionTemp.Dispose();
                HOperatorSet.GenRectangle1(out ho_RegionTemp, hv_Row1, hv_Col1, hv_Row2, hv_Col2);
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.ConcatObj(ho_RegionBanZi, ho_RegionTemp, out ExpTmpOutVar_0);
                    ho_RegionBanZi.Dispose();
                    ho_RegionBanZi = ExpTmpOutVar_0;
                }
            }

            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.SelectShape(ho_RegionBanZi, out ExpTmpOutVar_0, "area", "and", 1,
                    99900099);
                ho_RegionBanZi.Dispose();
                ho_RegionBanZi = ExpTmpOutVar_0;
            }
            hv_CountBanZi.Dispose();
            HOperatorSet.CountObj(ho_RegionBanZi, out hv_CountBanZi);


            hv_PlatePosX.Dispose();
            hv_PlatePosX = new HTuple();
            hv_PlatePosY.Dispose();
            hv_PlatePosY = new HTuple();
            hv_PeiDuPosX.Dispose();
            hv_PeiDuPosX = new HTuple();
            hv_PeiDuPosY.Dispose();
            hv_PeiDuPosY = new HTuple();

            hv_PlatePosXStr.Dispose();
            hv_PlatePosXStr = "PlatePosX:";
            hv_PlatePosYStr.Dispose();
            hv_PlatePosYStr = "PlatePosY:";




            //输出坐标
            HTuple end_val99 = hv_CountBanZi;
            HTuple step_val99 = 1;
            for (hv_Ind = 1; hv_Ind.Continue(end_val99, step_val99); hv_Ind = hv_Ind.TupleAdd(step_val99))
            {
                ho_ObjectSelected.Dispose();
                HOperatorSet.SelectObj(ho_RegionBanZi, out ho_ObjectSelected, hv_Ind);
                hv_Roww.Dispose();
                HOperatorSet.RegionFeatures(ho_ObjectSelected, "row", out hv_Roww);
                hv_Coll.Dispose();
                HOperatorSet.RegionFeatures(ho_ObjectSelected, "column", out hv_Coll);

                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PlatePosX = hv_PlatePosX.TupleConcat(
                            hv_Coll);
                        hv_PlatePosX.Dispose();
                        hv_PlatePosX = ExpTmpLocalVar_PlatePosX;
                    }
                }
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PlatePosY = hv_PlatePosY.TupleConcat(
                            hv_Roww);
                        hv_PlatePosY.Dispose();
                        hv_PlatePosY = ExpTmpLocalVar_PlatePosY;
                    }
                }

                hv_CollStr.Dispose();
                HOperatorSet.TupleString(hv_Coll, "10.2f", out hv_CollStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PlatePosXStr = hv_PlatePosXStr + hv_CollStr;
                        hv_PlatePosXStr.Dispose();
                        hv_PlatePosXStr = ExpTmpLocalVar_PlatePosXStr;
                    }
                }

                hv_RowwStr.Dispose();
                HOperatorSet.TupleString(hv_Roww, "10.2f", out hv_RowwStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PlatePosYStr = hv_PlatePosYStr + hv_RowwStr;
                        hv_PlatePosYStr.Dispose();
                        hv_PlatePosYStr = ExpTmpLocalVar_PlatePosYStr;
                    }
                }


            }

            hv_PeiDuPosXStr.Dispose();
            hv_PeiDuPosXStr = "PeiDuPosX:";
            hv_PeiDuPosYStr.Dispose();
            hv_PeiDuPosYStr = "PeiDuPosY:";

            if ((int)(hv_NeedPeiDuBan) != 0)
            {
                hv_Roww.Dispose();
                HOperatorSet.RegionFeatures(ho_RegionPeiDu1, "row", out hv_Roww);
                hv_Coll.Dispose();
                HOperatorSet.RegionFeatures(ho_RegionPeiDu1, "column2", out hv_Coll);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosX = hv_PeiDuPosX.TupleConcat(
                            hv_Coll);
                        hv_PeiDuPosX.Dispose();
                        hv_PeiDuPosX = ExpTmpLocalVar_PeiDuPosX;
                    }
                }
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosY = hv_PeiDuPosY.TupleConcat(
                            hv_Roww);
                        hv_PeiDuPosY.Dispose();
                        hv_PeiDuPosY = ExpTmpLocalVar_PeiDuPosY;
                    }
                }

                hv_CollStr.Dispose();
                HOperatorSet.TupleString(hv_Coll, "10.2f", out hv_CollStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosXStr = hv_PeiDuPosXStr + hv_CollStr;
                        hv_PeiDuPosXStr.Dispose();
                        hv_PeiDuPosXStr = ExpTmpLocalVar_PeiDuPosXStr;
                    }
                }

                hv_RowwStr.Dispose();
                HOperatorSet.TupleString(hv_Roww, "10.2f", out hv_RowwStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosYStr = hv_PeiDuPosYStr + hv_RowwStr;
                        hv_PeiDuPosYStr.Dispose();
                        hv_PeiDuPosYStr = ExpTmpLocalVar_PeiDuPosYStr;
                    }
                }

                hv_Roww.Dispose();
                HOperatorSet.RegionFeatures(ho_RegionPeiDu2, "row", out hv_Roww);
                hv_Coll.Dispose();
                HOperatorSet.RegionFeatures(ho_RegionPeiDu2, "column1", out hv_Coll);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosX = hv_PeiDuPosX.TupleConcat(
                            hv_Coll);
                        hv_PeiDuPosX.Dispose();
                        hv_PeiDuPosX = ExpTmpLocalVar_PeiDuPosX;
                    }
                }
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosY = hv_PeiDuPosY.TupleConcat(
                            hv_Roww);
                        hv_PeiDuPosY.Dispose();
                        hv_PeiDuPosY = ExpTmpLocalVar_PeiDuPosY;
                    }
                }

                hv_CollStr.Dispose();
                HOperatorSet.TupleString(hv_Coll, "10.2f", out hv_CollStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosXStr = hv_PeiDuPosXStr + hv_CollStr;
                        hv_PeiDuPosXStr.Dispose();
                        hv_PeiDuPosXStr = ExpTmpLocalVar_PeiDuPosXStr;
                    }
                }

                hv_RowwStr.Dispose();
                HOperatorSet.TupleString(hv_Roww, "10.2f", out hv_RowwStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosYStr = hv_PeiDuPosYStr + hv_RowwStr;
                        hv_PeiDuPosYStr.Dispose();
                        hv_PeiDuPosYStr = ExpTmpLocalVar_PeiDuPosYStr;
                    }
                }


            }


            //***************************


            HOperatorSet.DispObj(ho_ImageBackGround, hv_ExpDefaultWinHandle);

            if ((int)(new HTuple(hv_NeedPeiDuBan.TupleEqual(1))) != 0)
            {
                HOperatorSet.SetColor(hv_ExpDefaultWinHandle, "orange");
                HOperatorSet.DispObj(ho_RegionPeiDu1, hv_ExpDefaultWinHandle);
                HOperatorSet.DispObj(ho_RegionPeiDu2, hv_ExpDefaultWinHandle);
            }


            HOperatorSet.SetColored(hv_ExpDefaultWinHandle, 12);
            HOperatorSet.DispObj(ho_RegionBanZi, hv_ExpDefaultWinHandle);




            HOperatorSet.SetColor(hv_ExpDefaultWinHandle, "red");
            HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, 24, 12);

            if ((int)(new HTuple(hv_PeiDuExist.TupleEqual(1))) != 0)
            {
                hv_PeiDuExistStr.Dispose();
                hv_PeiDuExistStr = "是";
            }
            else
            {
                hv_PeiDuExistStr.Dispose();
                hv_PeiDuExistStr = "否";
            }


            hv_FreeWidthAll.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_FreeWidthAll = (hv_TotalWidth_COPY_INP_TMP - hv_WidthPanelAll) - ((hv_NeedPeiDuBan * 2) * hv_PeiDuWidth_COPY_INP_TMP);
            }
            hv_str1.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_str1 = (((((((("陪镀板:" + hv_NeedPeiDuBan) + "     板子数量:") + hv_CountBanZi) + "     底座调整：") + hv_BaseAdjustVal) + "     左右总空余:") + hv_FreeWidthAll) + "    是否存在陪镀板：") + hv_PeiDuExistStr;
            }

            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_str1);

            HOperatorSet.SetColor(hv_ExpDefaultWinHandle, "green");
            HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, 80, 12);
            hv_str.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_str = (((((((((((((("板宽:" + hv_PanelWidth_COPY_INP_TMP) + "    板高:") + hv_PanelHeight_COPY_INP_TMP) + "    板厚:") + hv_PanelThickness) + "    陪镀板宽:") + hv_PeiDuWidth_COPY_INP_TMP) + "   陪镀板高:") + hv_PeiDuHeight) + "    板间隙:") + hv_Interval_COPY_INP_TMP) + "    总长:") + hv_TotalWidth_COPY_INP_TMP) + "    占满阈值:") + hv_FullThres_COPY_INP_TMP;
            }

            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_str);







            HOperatorSet.SetColor(hv_ExpDefaultWinHandle, "pink");
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, hv_TotalHeight_COPY_INP_TMP - 100,
                    12);
            }
            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_PlatePosXStr);

            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, hv_TotalHeight_COPY_INP_TMP - 50,
                    12);
            }
            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_PlatePosYStr);

            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, hv_TotalHeight_COPY_INP_TMP - 100,
                    hv_TotalWidth_COPY_INP_TMP - 1100);
            }
            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_PeiDuPosXStr);

            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, hv_TotalHeight_COPY_INP_TMP - 50,
                    hv_TotalWidth_COPY_INP_TMP - 1100);
            }
            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_PeiDuPosYStr);





            ho_Image.Dispose();
            ho_ImageBackGround.Dispose();
            ho_RegionBanZiAll.Dispose();
            ho_RegionPeiDu1.Dispose();
            ho_RegionPeiDu2.Dispose();
            ho_RegionBanZi.Dispose();
            ho_RegionTemp.Dispose();
            ho_ObjectSelected.Dispose();

            hv_FullThres_COPY_INP_TMP.Dispose();
            hv_Interval_COPY_INP_TMP.Dispose();
            hv_PanelHeight_COPY_INP_TMP.Dispose();
            hv_PanelWidth_COPY_INP_TMP.Dispose();
            hv_PeiDuWidth_COPY_INP_TMP.Dispose();
            hv_TotalHeight_COPY_INP_TMP.Dispose();
            hv_TotalWidth_COPY_INP_TMP.Dispose();
            hv_countPanel.Dispose();
            hv_WidthPanelAll.Dispose();
            hv_NeedPeiDuBan.Dispose();
            hv_RowAll1.Dispose();
            hv_RowAll2.Dispose();
            hv_ColAll1.Dispose();
            hv_ColAll2.Dispose();
            hv_Index.Dispose();
            hv_Row1.Dispose();
            hv_Row2.Dispose();
            hv_Col1.Dispose();
            hv_Col2.Dispose();
            hv_CountBanZi.Dispose();
            hv_PlatePosXStr.Dispose();
            hv_PlatePosYStr.Dispose();
            hv_Ind.Dispose();
            hv_Roww.Dispose();
            hv_Coll.Dispose();
            hv_CollStr.Dispose();
            hv_RowwStr.Dispose();
            hv_PeiDuPosXStr.Dispose();
            hv_PeiDuPosYStr.Dispose();
            hv_PeiDuExistStr.Dispose();
            hv_FreeWidthAll.Dispose();
            hv_str1.Dispose();
            hv_str.Dispose();

            return;
        }


        public static void LayoutCalBan(HTuple hv_TotalWidth, HTuple hv_TotalHeight, HTuple hv_PanelWidth,
    HTuple hv_PanelHeight, HTuple hv_PanelCount, HTuple hv_PeiDuWidth, HTuple hv_FullThres,
    HTuple hv_Interval, HTuple hv_WindowHandle, HTuple hv_PeiDuHeight, HTuple hv_BaseAdjustVal,
    HTuple hv_PanelThickness, HTuple hv_PeiDuExist, out HTuple hv_PlatePosX, out HTuple hv_PlatePosY,
    out HTuple hv_PeiDuPosX, out HTuple hv_PeiDuPosY)
        {



            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_Image, ho_ImageBackGround, ho_RegionBanZiAll;
            HObject ho_RegionPeiDu1 = null, ho_RegionPeiDu2 = null, ho_RegionBanZi;
            HObject ho_RegionTemp = null, ho_ObjectSelected = null;

            // Local control variables 

            HTuple hv_countPanel = new HTuple(), hv_WidthPanelAll = new HTuple();
            HTuple hv_NeedPeiDuBan = new HTuple(), hv_RowAll1 = new HTuple();
            HTuple hv_RowAll2 = new HTuple(), hv_ColAll1 = new HTuple();
            HTuple hv_ColAll2 = new HTuple(), hv_Index = new HTuple();
            HTuple hv_Row1 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Col1 = new HTuple(), hv_Col2 = new HTuple();
            HTuple hv_CountBanZi = new HTuple(), hv_PlatePosXStr = new HTuple();
            HTuple hv_PlatePosYStr = new HTuple(), hv_Ind = new HTuple();
            HTuple hv_Roww = new HTuple(), hv_Coll = new HTuple();
            HTuple hv_CollStr = new HTuple(), hv_RowwStr = new HTuple();
            HTuple hv_PeiDuPosXStr = new HTuple(), hv_PeiDuPosYStr = new HTuple();
            HTuple hv_PeiDuExistStr = new HTuple(), hv_FreeWidthAll = new HTuple();
            HTuple hv_str1 = new HTuple(), hv_str = new HTuple();
            HTuple hv_FullThres_COPY_INP_TMP = new HTuple(hv_FullThres);
            HTuple hv_Interval_COPY_INP_TMP = new HTuple(hv_Interval);
            HTuple hv_PanelHeight_COPY_INP_TMP = new HTuple(hv_PanelHeight);
            HTuple hv_PanelWidth_COPY_INP_TMP = new HTuple(hv_PanelWidth);
            HTuple hv_PeiDuWidth_COPY_INP_TMP = new HTuple(hv_PeiDuWidth);
            HTuple hv_TotalHeight_COPY_INP_TMP = new HTuple(hv_TotalHeight);
            HTuple hv_TotalWidth_COPY_INP_TMP = new HTuple(hv_TotalWidth);

            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Image);
            HOperatorSet.GenEmptyObj(out ho_ImageBackGround);
            HOperatorSet.GenEmptyObj(out ho_RegionBanZiAll);
            HOperatorSet.GenEmptyObj(out ho_RegionPeiDu1);
            HOperatorSet.GenEmptyObj(out ho_RegionPeiDu2);
            HOperatorSet.GenEmptyObj(out ho_RegionBanZi);
            HOperatorSet.GenEmptyObj(out ho_RegionTemp);
            HOperatorSet.GenEmptyObj(out ho_ObjectSelected);
            hv_PlatePosX = new HTuple();
            hv_PlatePosY = new HTuple();
            hv_PeiDuPosX = new HTuple();
            hv_PeiDuPosY = new HTuple();

            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_PanelWidth = hv_PanelWidth_COPY_INP_TMP.TupleReal()
                        ;
                    hv_PanelWidth_COPY_INP_TMP.Dispose();
                    hv_PanelWidth_COPY_INP_TMP = ExpTmpLocalVar_PanelWidth;
                }
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_PanelHeight = hv_PanelHeight_COPY_INP_TMP.TupleReal()
                        ;
                    hv_PanelHeight_COPY_INP_TMP.Dispose();
                    hv_PanelHeight_COPY_INP_TMP = ExpTmpLocalVar_PanelHeight;
                }
            }

            //陪镀板宽度
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_PeiDuWidth = hv_PeiDuWidth_COPY_INP_TMP.TupleReal()
                        ;
                    hv_PeiDuWidth_COPY_INP_TMP.Dispose();
                    hv_PeiDuWidth_COPY_INP_TMP = ExpTmpLocalVar_PeiDuWidth;
                }
            }

            //占满留边阈值，理论值等于一个夹子的宽度 ，两边除二
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_FullThres = hv_FullThres_COPY_INP_TMP.TupleReal()
                        ;
                    hv_FullThres_COPY_INP_TMP.Dispose();
                    hv_FullThres_COPY_INP_TMP = ExpTmpLocalVar_FullThres;
                }
            }

            //区域总长宽，单位mm
            //宽度设置要考虑陪镀板余量
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_TotalWidth = hv_TotalWidth_COPY_INP_TMP.TupleReal()
                        ;
                    hv_TotalWidth_COPY_INP_TMP.Dispose();
                    hv_TotalWidth_COPY_INP_TMP = ExpTmpLocalVar_TotalWidth;
                }
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_TotalHeight = hv_TotalHeight_COPY_INP_TMP.TupleReal()
                        ;
                    hv_TotalHeight_COPY_INP_TMP.Dispose();
                    hv_TotalHeight_COPY_INP_TMP = ExpTmpLocalVar_TotalHeight;
                }
            }

            //板子之间的间隙，单位mm
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_Interval = hv_Interval_COPY_INP_TMP.TupleReal()
                        ;
                    hv_Interval_COPY_INP_TMP.Dispose();
                    hv_Interval_COPY_INP_TMP = ExpTmpLocalVar_Interval;
                }
            }



            //******************************
            //20250112,根据输入的板子个数，陪镀板是否存在和陪镀板宽度，计算排版


            //******************************************************************

            ho_Image.Dispose();
            HOperatorSet.GenImageConst(out ho_Image, "byte", hv_TotalWidth_COPY_INP_TMP,
                hv_TotalHeight_COPY_INP_TMP);
            ho_ImageBackGround.Dispose();
            HOperatorSet.PaintRegion(ho_Image, ho_Image, out ho_ImageBackGround, 128, "fill");



            //直接使用输入值

            hv_countPanel.Dispose();
            hv_countPanel = new HTuple(hv_PanelCount);
            //计算countPanel个板子总长
            hv_WidthPanelAll.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_WidthPanelAll = hv_countPanel * hv_PanelWidth_COPY_INP_TMP;
            }

            //检查是否需要陪镀板

            hv_NeedPeiDuBan.Dispose();
            hv_NeedPeiDuBan = 0;
            if ((int)(new HTuple(hv_PeiDuWidth_COPY_INP_TMP.TupleGreater(0))) != 0)
            {
                //满足占满条件
                hv_NeedPeiDuBan.Dispose();
                hv_NeedPeiDuBan = 1;

            }





            //计算板子整体区域，不含陪镀板
            hv_WidthPanelAll.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_WidthPanelAll = (hv_countPanel * hv_PanelWidth_COPY_INP_TMP) + ((hv_countPanel - 1) * hv_Interval_COPY_INP_TMP);
            }
            //*生成板子整体区域,高度方向居中
            hv_RowAll1.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_RowAll1 = (hv_TotalHeight_COPY_INP_TMP / 2) - (hv_PanelHeight_COPY_INP_TMP / 2);
            }
            hv_RowAll2.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_RowAll2 = (hv_TotalHeight_COPY_INP_TMP / 2) + (hv_PanelHeight_COPY_INP_TMP / 2);
            }
            hv_ColAll1.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_ColAll1 = (hv_TotalWidth_COPY_INP_TMP / 2) - (hv_WidthPanelAll / 2);
            }
            hv_ColAll2.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_ColAll2 = (hv_TotalWidth_COPY_INP_TMP / 2) + (hv_WidthPanelAll / 2);
            }
            ho_RegionBanZiAll.Dispose();
            HOperatorSet.GenRectangle1(out ho_RegionBanZiAll, hv_RowAll1, hv_ColAll1, hv_RowAll2,
                hv_ColAll2);

            //生成陪镀板
            if ((int)(new HTuple(hv_NeedPeiDuBan.TupleEqual(1))) != 0)
            {

                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_RegionPeiDu1.Dispose();
                    HOperatorSet.GenRectangle1(out ho_RegionPeiDu1, hv_RowAll1, (hv_ColAll1 - hv_PeiDuWidth_COPY_INP_TMP) - hv_Interval_COPY_INP_TMP,
                        hv_RowAll2, hv_ColAll1 - hv_Interval_COPY_INP_TMP);
                }
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    ho_RegionPeiDu2.Dispose();
                    HOperatorSet.GenRectangle1(out ho_RegionPeiDu2, hv_RowAll1, hv_ColAll2 + hv_Interval_COPY_INP_TMP,
                        hv_RowAll2, (hv_ColAll2 + hv_PeiDuWidth_COPY_INP_TMP) + hv_Interval_COPY_INP_TMP);
                }


            }



            //计算每个板子的区域
            ho_RegionBanZi.Dispose();
            HOperatorSet.GenEmptyRegion(out ho_RegionBanZi);
            HTuple end_val72 = hv_countPanel;
            HTuple step_val72 = 1;
            for (hv_Index = 1; hv_Index.Continue(end_val72, step_val72); hv_Index = hv_Index.TupleAdd(step_val72))
            {

                hv_Row1.Dispose();
                hv_Row1 = new HTuple(hv_RowAll1);
                hv_Row2.Dispose();
                hv_Row2 = new HTuple(hv_RowAll2);
                hv_Col1.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Col1 = (hv_ColAll1 + ((hv_Index - 1) * hv_PanelWidth_COPY_INP_TMP)) + ((hv_Index - 1) * hv_Interval_COPY_INP_TMP);
                }
                hv_Col2.Dispose();
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    hv_Col2 = hv_Col1 + hv_PanelWidth_COPY_INP_TMP;
                }

                ho_RegionTemp.Dispose();
                HOperatorSet.GenRectangle1(out ho_RegionTemp, hv_Row1, hv_Col1, hv_Row2, hv_Col2);
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.ConcatObj(ho_RegionBanZi, ho_RegionTemp, out ExpTmpOutVar_0);
                    ho_RegionBanZi.Dispose();
                    ho_RegionBanZi = ExpTmpOutVar_0;
                }
            }

            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.SelectShape(ho_RegionBanZi, out ExpTmpOutVar_0, "area", "and", 1,
                    99900099);
                ho_RegionBanZi.Dispose();
                ho_RegionBanZi = ExpTmpOutVar_0;
            }
            hv_CountBanZi.Dispose();
            HOperatorSet.CountObj(ho_RegionBanZi, out hv_CountBanZi);


            hv_PlatePosX.Dispose();
            hv_PlatePosX = new HTuple();
            hv_PlatePosY.Dispose();
            hv_PlatePosY = new HTuple();
            hv_PeiDuPosX.Dispose();
            hv_PeiDuPosX = new HTuple();
            hv_PeiDuPosY.Dispose();
            hv_PeiDuPosY = new HTuple();

            hv_PlatePosXStr.Dispose();
            hv_PlatePosXStr = "PlatePosX:";
            hv_PlatePosYStr.Dispose();
            hv_PlatePosYStr = "PlatePosY:";




            //输出坐标
            HTuple end_val99 = hv_CountBanZi;
            HTuple step_val99 = 1;
            for (hv_Ind = 1; hv_Ind.Continue(end_val99, step_val99); hv_Ind = hv_Ind.TupleAdd(step_val99))
            {
                ho_ObjectSelected.Dispose();
                HOperatorSet.SelectObj(ho_RegionBanZi, out ho_ObjectSelected, hv_Ind);
                hv_Roww.Dispose();
                HOperatorSet.RegionFeatures(ho_ObjectSelected, "row", out hv_Roww);
                hv_Coll.Dispose();
                HOperatorSet.RegionFeatures(ho_ObjectSelected, "column", out hv_Coll);

                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PlatePosX = hv_PlatePosX.TupleConcat(
                            hv_Coll);
                        hv_PlatePosX.Dispose();
                        hv_PlatePosX = ExpTmpLocalVar_PlatePosX;
                    }
                }
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PlatePosY = hv_PlatePosY.TupleConcat(
                            hv_Roww);
                        hv_PlatePosY.Dispose();
                        hv_PlatePosY = ExpTmpLocalVar_PlatePosY;
                    }
                }

                hv_CollStr.Dispose();
                HOperatorSet.TupleString(hv_Coll, "10.2f", out hv_CollStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PlatePosXStr = hv_PlatePosXStr + hv_CollStr;
                        hv_PlatePosXStr.Dispose();
                        hv_PlatePosXStr = ExpTmpLocalVar_PlatePosXStr;
                    }
                }

                hv_RowwStr.Dispose();
                HOperatorSet.TupleString(hv_Roww, "10.2f", out hv_RowwStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PlatePosYStr = hv_PlatePosYStr + hv_RowwStr;
                        hv_PlatePosYStr.Dispose();
                        hv_PlatePosYStr = ExpTmpLocalVar_PlatePosYStr;
                    }
                }


            }

            hv_PeiDuPosXStr.Dispose();
            hv_PeiDuPosXStr = "PeiDuPosX:";
            hv_PeiDuPosYStr.Dispose();
            hv_PeiDuPosYStr = "PeiDuPosY:";

            if ((int)(hv_NeedPeiDuBan) != 0)
            {
                hv_Roww.Dispose();
                HOperatorSet.RegionFeatures(ho_RegionPeiDu1, "row", out hv_Roww);
                hv_Coll.Dispose();
                HOperatorSet.RegionFeatures(ho_RegionPeiDu1, "column2", out hv_Coll);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosX = hv_PeiDuPosX.TupleConcat(
                            hv_Coll);
                        hv_PeiDuPosX.Dispose();
                        hv_PeiDuPosX = ExpTmpLocalVar_PeiDuPosX;
                    }
                }
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosY = hv_PeiDuPosY.TupleConcat(
                            hv_Roww);
                        hv_PeiDuPosY.Dispose();
                        hv_PeiDuPosY = ExpTmpLocalVar_PeiDuPosY;
                    }
                }

                hv_CollStr.Dispose();
                HOperatorSet.TupleString(hv_Coll, "10.2f", out hv_CollStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosXStr = hv_PeiDuPosXStr + hv_CollStr;
                        hv_PeiDuPosXStr.Dispose();
                        hv_PeiDuPosXStr = ExpTmpLocalVar_PeiDuPosXStr;
                    }
                }

                hv_RowwStr.Dispose();
                HOperatorSet.TupleString(hv_Roww, "10.2f", out hv_RowwStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosYStr = hv_PeiDuPosYStr + hv_RowwStr;
                        hv_PeiDuPosYStr.Dispose();
                        hv_PeiDuPosYStr = ExpTmpLocalVar_PeiDuPosYStr;
                    }
                }

                hv_Roww.Dispose();
                HOperatorSet.RegionFeatures(ho_RegionPeiDu2, "row", out hv_Roww);
                hv_Coll.Dispose();
                HOperatorSet.RegionFeatures(ho_RegionPeiDu2, "column1", out hv_Coll);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosX = hv_PeiDuPosX.TupleConcat(
                            hv_Coll);
                        hv_PeiDuPosX.Dispose();
                        hv_PeiDuPosX = ExpTmpLocalVar_PeiDuPosX;
                    }
                }
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosY = hv_PeiDuPosY.TupleConcat(
                            hv_Roww);
                        hv_PeiDuPosY.Dispose();
                        hv_PeiDuPosY = ExpTmpLocalVar_PeiDuPosY;
                    }
                }

                hv_CollStr.Dispose();
                HOperatorSet.TupleString(hv_Coll, "10.2f", out hv_CollStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosXStr = hv_PeiDuPosXStr + hv_CollStr;
                        hv_PeiDuPosXStr.Dispose();
                        hv_PeiDuPosXStr = ExpTmpLocalVar_PeiDuPosXStr;
                    }
                }

                hv_RowwStr.Dispose();
                HOperatorSet.TupleString(hv_Roww, "10.2f", out hv_RowwStr);
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    {
                        HTuple
                          ExpTmpLocalVar_PeiDuPosYStr = hv_PeiDuPosYStr + hv_RowwStr;
                        hv_PeiDuPosYStr.Dispose();
                        hv_PeiDuPosYStr = ExpTmpLocalVar_PeiDuPosYStr;
                    }
                }


            }


            //***************************


            HOperatorSet.DispObj(ho_ImageBackGround, hv_ExpDefaultWinHandle);

            if ((int)(new HTuple(hv_NeedPeiDuBan.TupleEqual(1))) != 0)
            {
                HOperatorSet.SetColor(hv_ExpDefaultWinHandle, "orange");
                HOperatorSet.DispObj(ho_RegionPeiDu1, hv_ExpDefaultWinHandle);
                HOperatorSet.DispObj(ho_RegionPeiDu2, hv_ExpDefaultWinHandle);
            }


            HOperatorSet.SetColored(hv_ExpDefaultWinHandle, 12);
            HOperatorSet.DispObj(ho_RegionBanZi, hv_ExpDefaultWinHandle);




            HOperatorSet.SetColor(hv_ExpDefaultWinHandle, "red");
            HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, 24, 12);

            if ((int)(new HTuple(hv_PeiDuExist.TupleEqual(1))) != 0)
            {
                hv_PeiDuExistStr.Dispose();
                hv_PeiDuExistStr = "是";
            }
            else
            {
                hv_PeiDuExistStr.Dispose();
                hv_PeiDuExistStr = "否";
            }


            hv_FreeWidthAll.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_FreeWidthAll = (hv_TotalWidth_COPY_INP_TMP - hv_WidthPanelAll) - ((hv_NeedPeiDuBan * 2) * hv_PeiDuWidth_COPY_INP_TMP);
            }
            hv_str1.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_str1 = (((((((("陪镀板:" + hv_NeedPeiDuBan) + "     板子数量:") + hv_CountBanZi) + "     底座调整：") + hv_BaseAdjustVal) + "     左右总空余:") + hv_FreeWidthAll) + "    是否存在陪镀板：") + hv_PeiDuExistStr;
            }

            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_str1);

            HOperatorSet.SetColor(hv_ExpDefaultWinHandle, "green");
            HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, 80, 12);
            hv_str.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_str = (((((((((((((("板宽:" + hv_PanelWidth_COPY_INP_TMP) + "    板高:") + hv_PanelHeight_COPY_INP_TMP) + "    板厚:") + hv_PanelThickness) + "    陪镀板宽:") + hv_PeiDuWidth_COPY_INP_TMP) + "   陪镀板高:") + hv_PeiDuHeight) + "    板间隙:") + hv_Interval_COPY_INP_TMP) + "    总长:") + hv_TotalWidth_COPY_INP_TMP) + "    占满阈值:") + hv_FullThres_COPY_INP_TMP;
            }

            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_str);







            HOperatorSet.SetColor(hv_ExpDefaultWinHandle, "pink");
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, hv_TotalHeight_COPY_INP_TMP - 100,
                    12);
            }
            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_PlatePosXStr);

            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, hv_TotalHeight_COPY_INP_TMP - 50,
                    12);
            }
            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_PlatePosYStr);

            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, hv_TotalHeight_COPY_INP_TMP - 100,
                    hv_TotalWidth_COPY_INP_TMP - 1100);
            }
            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_PeiDuPosXStr);

            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                HOperatorSet.SetTposition(hv_ExpDefaultWinHandle, hv_TotalHeight_COPY_INP_TMP - 50,
                    hv_TotalWidth_COPY_INP_TMP - 1100);
            }
            HOperatorSet.WriteString(hv_ExpDefaultWinHandle, hv_PeiDuPosYStr);





            ho_Image.Dispose();
            ho_ImageBackGround.Dispose();
            ho_RegionBanZiAll.Dispose();
            ho_RegionPeiDu1.Dispose();
            ho_RegionPeiDu2.Dispose();
            ho_RegionBanZi.Dispose();
            ho_RegionTemp.Dispose();
            ho_ObjectSelected.Dispose();

            hv_FullThres_COPY_INP_TMP.Dispose();
            hv_Interval_COPY_INP_TMP.Dispose();
            hv_PanelHeight_COPY_INP_TMP.Dispose();
            hv_PanelWidth_COPY_INP_TMP.Dispose();
            hv_PeiDuWidth_COPY_INP_TMP.Dispose();
            hv_TotalHeight_COPY_INP_TMP.Dispose();
            hv_TotalWidth_COPY_INP_TMP.Dispose();
            hv_countPanel.Dispose();
            hv_WidthPanelAll.Dispose();
            hv_NeedPeiDuBan.Dispose();
            hv_RowAll1.Dispose();
            hv_RowAll2.Dispose();
            hv_ColAll1.Dispose();
            hv_ColAll2.Dispose();
            hv_Index.Dispose();
            hv_Row1.Dispose();
            hv_Row2.Dispose();
            hv_Col1.Dispose();
            hv_Col2.Dispose();
            hv_CountBanZi.Dispose();
            hv_PlatePosXStr.Dispose();
            hv_PlatePosYStr.Dispose();
            hv_Ind.Dispose();
            hv_Roww.Dispose();
            hv_Coll.Dispose();
            hv_CollStr.Dispose();
            hv_RowwStr.Dispose();
            hv_PeiDuPosXStr.Dispose();
            hv_PeiDuPosYStr.Dispose();
            hv_PeiDuExistStr.Dispose();
            hv_FreeWidthAll.Dispose();
            hv_str1.Dispose();
            hv_str.Dispose();

            return;
        }



























    }
}
