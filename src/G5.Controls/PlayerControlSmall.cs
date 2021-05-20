using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using G5.Logic;
using System.Resources;


namespace G5.Controls
{
    public partial class PlayerControlSmall : UserControl
    {
        public PlayerControlSmall()
        {
            InitializeComponent();
        }

        private static Bitmap getResImage(string str)
        {
            object obj = Resources.ResourceManager.GetObject(str, Resources.Culture);
            return (Bitmap)(obj);
        }

        public void updatePlayerInfo(string name, int stack, Status statusInHand, HoleCards holeCards, Position position, bool toAct)
        {
            labelName.Text = name + " (" + position.ToString() + ")";
            labelStack.Text = "$" + (stack / 100.0f).ToString("f2");
            labelStatus.Text = statusInHand.ToString();
            labelPosition.Text = ""; // position.ToString();

            pictureHH1.Visible = (statusInHand != Status.Folded);
            pictureHH2.Visible = (statusInHand != Status.Folded);

            if (holeCards == null)
            {
                pictureHH1.Image = Resources._card_back;
                pictureHH2.Image = Resources._card_back;
            }
            else
            {
                pictureHH1.Image = getResImage("_" + holeCards.Card0.ToString());
                pictureHH2.Image = getResImage("_" + holeCards.Card1.ToString());
            }

            this.BackColor = (toAct) ? Color.LightGray : Color.DimGray;
        }
    }
}
