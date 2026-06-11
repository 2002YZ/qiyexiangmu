using HalconDotNet;
using Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;


namespace PanelHandingSystem
{

    //全局参数管理
    public static class ParaSet
    {

        public  class Recipe  //软件设定参数配方
        {

            //PLC
            public string PLCIPAddr = "192.168.3.236";
            public int PLCIPPort = 502;
            public int PLCRegisterStartAddr = 7100;
            public int PLCAddrOffSet = 8192;

            //MES
            public string MESIPAddr = "10.3.46.90";
            public int MESIPPort = 5012;

          
            //线体号    设备号
           public  string myLinecode = "F2_LM02L";
           public string myMachinecode = "D06114344-00";

            //传送带扫码枪 ,两个
            
            public string BarCodeIPAddr1 = "192.168.3.40";   
            public int BarCodeIPPort1 = 2001;

            public string BarCodeIPAddr2 = "192.168.3.41";
            public int BarCodeIPPort2 = 2001;

            //飞巴扫扫码
            public string BarCodeIPAddr3 = "192.168.3.245";
            public int BarCodeIPPort3 = 2001;


            public string BarCodeIPAddr4 = "192.168.3.244";
            public int BarCodeIPPort4 = 2001;


            //传送带扫码枪新增两个

            public string BarCodeIPAddr5 = "192.168.3.11";
            public int BarCodeIPPort5 = 2001;

            public string BarCodeIPAddr6 = "192.168.3.12";
            public int BarCodeIPPort6 = 2001;



            //梅卡Vision系统
            public string MechVisionIPAddr = "127.0.0.1";
            public  int MechVisionIPPort = 60000;

            //海康VisionMaster系统
            public  string HikVMIPAddr = "127.0.0.1";
            public  int HikVMIPPort = 7930;

            //机器人1 ---左
            public  string RobotIPAddr1 = "192.168.3.60";
            public int RobotIPPort1 = 502;
       

            //机器人2----右
            public string RobotIPAddr2 = "192.168.3.61";
            public int RobotIPPort2 = 502;

            //机器人1寄存器地址起始点和偏移点
            public int RobotRegisterStartAddr1 = 0;
            public int RobotAddrOffSet1 = 0;

            //机器人2寄存器地址起始点和偏移点
            public int RobotRegisterStartAddr2 = 0;
            public int RobotAddrOffSet2 = 0;

            /// <summary>
            /// 以下为系统固定参数
            /// </summary>
            //上料区域总宽，高
            public float TotalWidth = 3550;
            public float TotalHeight = 1000;

            //陪镀板宽度
            public float PeiDuWidth = 100;

            //占满留边阈值，理论值等于一个夹子的宽度 ，两边除二
            public float FullThres = 180;

            //板子之间的间隙，单位mm
            public float PanelInterval = 0;


            //飞巴坐标偏移
            public float FeiBaPosOffSet = -90 ;
            //力控

            public string ForceIp1 = "192.168.3.50";
            public int ForcePort1 = 504;

            public string ForceIp2 = "192.168.3.51";
            public int ForcePort2 = 505;

            //力控6个数的阈值，绝对值
            public double ForceThres1 = 505.0;
            public double ForceThres2 = 505.0;
            public double ForceThres3 = 505.0;
            public double ForceThres4 = 505.0;
            public double ForceThres5 = 505.0;
            public double ForceThres6 = 505.0;

        }



        public static Recipe recipe = new Recipe();
        public static string ParamPath = ".\\Param\\";
    


        public static void InitParaSet()
        {

            try
            {
                LogRecord.addLog("参数文件夹:" + ParamPath);
                LogRecord.addLog("载入json参数");
                string jsonContent = File.ReadAllText(ParamPath  + "ParaSet.json");
                recipe = Json2Obj<Recipe>(jsonContent);     
            }
            catch (Exception ex)
            {

                LogRecord.addLog("载入参数出错" + ex.Message);
            }
        }



        public static void PrintParaSet()
        {
            LogRecord.addLog($"扫码枪1 IP:{recipe.BarCodeIPAddr1},Port:{recipe.BarCodeIPPort1}" );
            LogRecord.addLog($"扫码枪2 IP:{recipe.BarCodeIPAddr2},Port:{recipe.BarCodeIPPort2}" );

            LogRecord.addLog($"机器人1 IP:{recipe.RobotIPAddr1},Port:{recipe.RobotIPPort1}");
            LogRecord.addLog($"机器人2 IP:{recipe.RobotIPAddr2},Port:{recipe.RobotIPPort2}");

            LogRecord.addLog($"Mes IP:{recipe.MESIPAddr},Port:{recipe.MESIPPort}");
            LogRecord.addLog($"PLC IP:{recipe.PLCIPAddr},Port:{recipe.PLCIPPort}");


            LogRecord.addLog($"2D VisionMaster IP:{recipe.HikVMIPAddr},Port:{recipe.HikVMIPPort}");
            LogRecord.addLog($"3D Vision IP:{recipe.MechVisionIPAddr},Port:{recipe.MechVisionIPPort}");



            LogRecord.addLog($"上料区域总宽:{recipe.TotalWidth}");
            LogRecord.addLog($"上料区域总高:{recipe.TotalHeight}");
            LogRecord.addLog($"陪镀板宽度:{recipe.PeiDuWidth}");
            LogRecord.addLog($"占满阈值:{recipe.FullThres}");
            LogRecord.addLog($"板间间隙距离:{recipe.PanelInterval}");

        }




        public static void Savejson(Recipe _recipe)
        {
            //检查文件名是否存在,存在弹框提示
            string rootPath = Path.GetPathRoot(Environment.SystemDirectory);
            string Recipefilename =  ParamPath  + "ParaSet.json";


            if (File.Exists(Recipefilename))
            {

                DialogResult result = MessageBox.Show("文件已经存在,是否覆盖？", "是", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // 用户点击了“是”按钮
                    string jsonStringAll = Obj2Json<Recipe>(_recipe);
                    File.WriteAllText(Recipefilename, jsonStringAll);//将内容写进json文件中


                    LogRecord.addLog(Recipefilename + "参数已被覆盖");



                }
                else
                {
                    // 用户点击了“否”按钮
                    LogRecord.addLog("用户取消保存");
                }



            }
            else
            {
                string jsonStringAll = Obj2Json<Recipe>(_recipe);
                File.WriteAllText(Recipefilename, jsonStringAll);//将内容写进json文件中

            }
        }

        public static void SaveJsonFile(string filename,string jsonstr)
        {
            try
            {

                File.WriteAllText(filename, jsonstr);//将内容写进json文件中

            }
            catch(Exception ex)
            {
                LogRecord.addLog("保存json文件出错" + ex.Message);

            }





        }





        public static T Json2Obj<T>(string json)
        {
            T obj = Activator.CreateInstance<T>();
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            {
                System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(obj.GetType());
                return (T)serializer.ReadObject(ms);
            }
        }


        public static string Obj2Json<T>(T data)
        {
            try
            {
                System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(data.GetType());
                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, data);
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            catch
            {
                return null;
            }
        }


    }
}
