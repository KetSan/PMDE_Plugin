//#define MODE_EXCLUSIVE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//---------------------------------------------------------------------------------
using System.Windows.Forms;
using PEPlugin;
using PEPlugin.Form;
using PEPlugin.Pmd;
using PEPlugin.Pmx;
using PEPlugin.SDX;
using PEPlugin.View;
using PEPlugin.Vmd;
using PEPlugin.Vme;
//---------------------------------------------------------------------------------



namespace TestPMDE
{
    public class CSScriptClass : PEPluginClass
    {
        /// <summary>
        /// PMDEditerを操作するために必要な変数群
        /// </summary>
        //-----------------------------------------------------------ここから-----------------------------------------------------------
        public IPEPluginHost host;
        public IPEBuilder builder;
        public IPEShortBuilder bd;
        public IPEConnector connect;
        public IPEXPmd pex;
        public IPXPmx PMX;
        public IPEPmd PMD;
        public IPEFormConnector Form;
//        public IPXPmxViewConnector PMXView;
        public IPEPMDViewConnector PMDView;
        //-----------------------------------------------------------ここまで-----------------------------------------------------------

        // コンストラクタ
        public CSScriptClass()
            : base()
        {
            // 起動オプション
            // boot時実行(true/false), プラグインメニューへの登録(true/false), メニュー登録名("")
#if MODE_EXCLUSIVE
            m_option = new PEPluginOption(false, true, "ウェイト自動設定(排他的)");
#else
            m_option = new PEPluginOption(false, true, "ウェイト自動設定(補完的)");
#endif
        }
        // エントリポイント　
        public override void Run(IPERunArgs args)
        {
            try
            {
                //PMD/PMXファイルを操作するためにおまじない。
                this.host = args.Host;
                this.builder = this.host.Builder;
                this.bd = this.host.Builder.SC;
                this.connect = this.host.Connector;
                this.pex = this.connect.Pmd.GetCurrentStateEx();
                this.PMD = this.connect.Pmd.GetCurrentState();
                this.PMX = this.connect.Pmx.GetCurrentState();
                this.Form = this.connect.Form;
                this.PMDView = this.connect.View.PMDView;
                

                //-----------------------------------------------------------ここから-----------------------------------------------------------
                //ここから処理開始
                //-----------------------------------------------------------ここから-----------------------------------------------------------


                if (this.connect.Form.PmxFormActivate)
                {
                    for (int i = 0; i < this.PMX.Vertex.Count; i++)
                    {
                        IPXVertex vertex = this.PMX.Vertex[i];
                        V3 vp = (V3)vertex.Position;
                        int ind1, ind2;
                        ind1 = ind2 = -1;
                        float dis1, dis2;
                        dis1 = dis2 = 10000000;
                        for (int j = 0; j < this.PMX.Bone.Count; j++)
                        {
                            IPXBone bone = this.PMX.Bone[j];
                            V3 bp = (V3)bone.Position;
                            float dis;
                            if (bone.ToBone == null) continue;
                            else dis = getDistanceBoneToVertex(bone, vertex);
                            if (dis < dis1)
                            {
                                dis2 = dis1;
                                ind2 = ind1;
                                ind1 = j;
                                dis1 = dis;
                            }
                            else if (dis < dis2)
                            {
                                dis2 = dis;
                                ind2 = j;
                            }
                        }

                        if (ind1 >= 0)
                        {
                            vertex.Bone1 = this.PMX.Bone[ind1];
                            vertex.Weight1 = 1.0f;
                        }

                        if (ind2 >= 0)
                        {
                            vertex.Bone2 = this.PMX.Bone[ind2];
#if MODE_EXCLUSIVE
                            vertex.Weight2 = 0f;
#else
                            vertex.Weight2 = (1f * dis1 / (dis1 + dis2));
                            vertex.Weight1 = 1.0f - vertex.Weight2;
#endif
                        }

                    }
                }
                else
                {
                    for (int i = 0; i < this.PMD.Vertex.Count; i++)
                    {
                        IPEVertex vertex = this.PMD.Vertex[i];
                        V3 vp = (V3)vertex.Position;
                        int ind1, ind2;
                        ind1 = ind2 = -1;
                        float dis1, dis2;
                        dis1 = dis2 = 10000000;
                        for (int j = 0; j < this.PMD.Bone.Count; j++)
                        {
                            IPEBone bone = this.PMD.Bone[j];
                            V3 bp = (V3)bone.Position;
                            float dis;
                            if (bone.To == -1 || bone.To == 0) continue;
                            else dis = getDistanceBoneToVertex(bone, vertex);
                            //                        float dis = (bp - vp).Length();
                            if (dis < dis1)
                            {
                                dis2 = dis1;
                                ind2 = ind1;
                                ind1 = j;
                                dis1 = dis;
                            }
                            else if (dis < dis2)
                            {
                                dis2 = dis;
                                ind2 = j;
                            }
                        }

                        if (ind1 >= 0)
                            vertex.Bone1 = ind1;
                        if (ind2 >= 0)
                        {
                            vertex.Bone2 = ind2;
#if MODE_EXCLUSIVE
                            vertex.Weight = 100;
#else
                            vertex.Weight = (int)(100f * dis2 / (dis1 + dis2));
#endif
                        }

                    }
                }


                //-----------------------------------------------------------ここまで-----------------------------------------------------------
                //処理ここまで
                //-----------------------------------------------------------ここまで-----------------------------------------------------------
                //モデル・画面を更新します。
                this.Update();
#if MODE_EXCLUSIVE
                MessageBox.Show(this.PMD.Vertex.Count.ToString() + "個の頂点のウェイトを最短排他形式で設定しました。",
                    "ウェイト自動設定(排他的)", MessageBoxButtons.OK, MessageBoxIcon.Information);
#else
                MessageBox.Show(this.PMX.Vertex.Count.ToString() + "個の頂点のウェイトを中間補完形式で設定しました。",
                    "ウェイト自動設定(補完的)", MessageBoxButtons.OK, MessageBoxIcon.Information);
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        /// <summary>
        /// モデル・画面を更新します。
        /// </summary>
        public void Update()
        {
            if (this.Form.PmxFormActivate)
            {
                this.connect.Pmx.Update(this.PMX);
            }
            else
            {
                this.connect.Pmd.Update(this.PMD);
            }
            this.connect.Form.UpdateList(UpdateObject.All);
            this.connect.View.PMDView.UpdateModel();
            this.connect.View.PMDView.UpdateView();
        }

        public float getDistanceBoneToVertex(IPXBone bone, IPXVertex vertex)
        {
            if (bone.ToBone != null)
            {
                V3 from = (V3)bone.Position;
                V3 to = (V3)bone.ToBone.Position;
                V3 v = (V3)vertex.Position;

                float d1, d2, d3;
                d1 = (v - from).Length();
                d2 = (v - to).Length();
                V3 line = to - from;
                V3 dif = v - from;
                if (line.Length() > 0.00001f)
                {
                    d3 = (Cross(line, dif)).Length() / line.Length();
                }
                else
                {
                    d3 = 100000f;
                }

                if (line.Length() > 0)
                {
                    float difDot = Dot(line, dif) / line.Length();
                    //垂線の足がボーン上
                    if (difDot > 0 && difDot < line.Length())
                    {
                        return Math.Min(d1, Math.Min(d2, d3));
                    }
                }


                return Math.Min(d1, d2);
            }
            else
            {
                V3 dif = (V3)((V3)vertex.Position - (V3)bone.Position);
                return dif.Length();
            }
        }

        public float getDistanceBoneToVertex(IPEBone bone, IPEVertex vertex)
        {
            if (bone.To != -1 && bone.To != 0)
            {
                V3 from = (V3)bone.Position;
                V3 to = (V3)this.PMD.Bone[bone.To].Position;
                V3 v = (V3)vertex.Position;

                float d1, d2, d3;
                d1 = (v - from).Length();
                d2 = (v - to).Length();
                V3 line = to - from;
                V3 dif = v - from;
                if (line.Length() > 0.00001f)
                {
                    d3 = (Cross(line, dif)).Length() / line.Length();
                }
                else
                {
                    d3 = 100000f;
                }

                if (line.Length() > 0)
                {
                    float difDot = Dot(line, dif) / line.Length();
                    //垂線の足がボーン上
                    if (difDot > 0 && difDot < line.Length())
                    {
                        return Math.Min(d1, Math.Min(d2, d3) );
                    }
                }


                return Math.Min(d1, d2);
            }
            else
            {
                V3 dif = (V3)((V3)vertex.Position - (V3)bone.Position);
                return dif.Length();
            }
        }

        public static float Dot(V3 a, V3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static V3 Cross( V3 a, V3 b )
        {
            V3 v = new V3(a.Y * b.Z - b.Y * a.Z, a.Z * b.X - b.Z * a.X, a.X * b.Y - b.X * a.Y);
            return v;
        }

    }
    //-----------------------------------------------------------------------------------

}
