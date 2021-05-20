using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using G5.Logic;
using System.Diagnostics;


namespace G5.Controls
{
    public partial class GameTableControl : UserControl
    {
        private PlayerControlSmall[] _playerControls = new PlayerControlSmall[6];
        private PictureBox[] _buttonImages = new PictureBox[6];
        private Label[] _labelsInPot = new Label[6];
        private string _raiseText;

        public event EventHandler NextButtonPressed;
        public event EventHandler FoldButtonPressed;
        public event EventHandler CallButtonPressed;
        public event EventHandler<int> RaiseButtonPressed;

        public GameTableControl()
        {
            InitializeComponent();

            _playerControls[0] = playerControlSmall0;
            _playerControls[1] = playerControlSmall1;
            _playerControls[2] = playerControlSmall2;
            _playerControls[3] = playerControlSmall3;
            _playerControls[4] = playerControlSmall4;
            _playerControls[5] = playerControlSmall5;

            _buttonImages[0] = pictureBoxButton0;
            _buttonImages[1] = pictureBoxButton1;
            _buttonImages[2] = pictureBoxButton2;
            _buttonImages[3] = pictureBoxButton3;
            _buttonImages[4] = pictureBoxButton4;
            _buttonImages[5] = pictureBoxButton5;

            _labelsInPot[0] = labelInPot0;
            _labelsInPot[1] = labelInPot1;
            _labelsInPot[2] = labelInPot2;
            _labelsInPot[3] = labelInPot3;
            _labelsInPot[4] = labelInPot4;
            _labelsInPot[5] = labelInPot5;
        }

        private Bitmap cardImage(Card card)
        {
            object obj = Resources.ResourceManager.GetObject("_" + card.ToString(), Resources.Culture);
            return (Bitmap)(obj);
        }

        public void displayBoard(List<Card> boardCards)
        {
            pictureFlop1.Visible = boardCards.Count > 0;
            pictureFlop2.Visible = boardCards.Count > 1;
            pictureFlop3.Visible = boardCards.Count > 2;
            pictureTurn.Visible = boardCards.Count > 3;
            pictureRiver.Visible = boardCards.Count > 4;

            if (boardCards.Count > 0)
                pictureFlop1.Image = cardImage(boardCards[0]);

            if (boardCards.Count > 1)
                pictureFlop2.Image = cardImage(boardCards[1]);

            if (boardCards.Count > 2)
                pictureFlop3.Image = cardImage(boardCards[2]);

            if (boardCards.Count > 3)
                pictureTurn.Image = cardImage(boardCards[3]);

            if (boardCards.Count > 4)
                pictureRiver.Image = cardImage(boardCards[4]);
        }

        private string moneyToString(int money)
        {
            return "$" + (money / 100.0f).ToString("f2");
        }

        public void hidePlayerInfo(int playerId)
        {
            _playerControls[playerId].Visible = false;
            _labelsInPot[playerId].Visible = false;
        }

        public void updatePlayerInfo(int playerId, string name, int stack, int moneyInPot, Status statusInHand, HoleCards holeCards, 
            Position preFlopPosition, bool toAct)
        {
            _playerControls[playerId].Visible = true;
            _playerControls[playerId].updatePlayerInfo(name, stack, statusInHand, holeCards, preFlopPosition, toAct);
            _labelsInPot[playerId].Text = moneyToString(moneyInPot);
            _labelsInPot[playerId].Visible = (moneyInPot != 0);
        }

        public void setButtonPosition(int pos)
        {
            foreach (var buttonImage in _buttonImages)
                buttonImage.Visible = false;

            _buttonImages[pos].Visible = true;
        }

        public void setPotSize(int potSize)
        {
            labelPot.Visible = (potSize != 0);
            labelPot.Text = "Pot: " + moneyToString(potSize);
        }

        public void disablePlayerControls()
        {
            buttonFold.Enabled = false;
            buttonCheckCall.Enabled = false;
            buttonBetRaise.Enabled = false;
            _trackBarRaiseAmm.Enabled = false;
        }

        public void enablePlayerControls()
        {
            buttonFold.Enabled = true;
            buttonCheckCall.Enabled = true;
            buttonBetRaise.Enabled = true;
            _trackBarRaiseAmm.Enabled = true;
        }

        public void setbuttonNextEnabled(bool enabled)
        {
            buttonNext.Enabled = enabled;
        }

        public void setupPlayerControls(int numBets, int ammountToCall, int defaultRaiseAmmount, int stackSize)
        {
            _raiseText = (numBets > 0) ? "Raise " : "Bet ";
            buttonBetRaise.Text = _raiseText;
            buttonCheckCall.Text = (ammountToCall > 0) ? ("Call " + moneyToString(ammountToCall)) : "Check";
            buttonFold.Enabled = ammountToCall > 0;

            _trackBarRaiseAmm.Minimum = ammountToCall + 4;
            _trackBarRaiseAmm.Maximum = stackSize;
            _trackBarRaiseAmm.TickFrequency = 1;
            _trackBarRaiseAmm.Value = (defaultRaiseAmmount < stackSize) ? defaultRaiseAmmount : stackSize;

            // Can raise if ammount to call is smaller than stack...
            buttonBetRaise.Enabled = ammountToCall < stackSize;
            _trackBarRaiseAmm.Enabled = ammountToCall < stackSize;

            trackBarRaiseAmm_Scroll(null, null);
        }

        public void log(string str)
        {
            listBoxLog.Items.Add(str);
            listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            NextButtonPressed?.Invoke(sender, e);
        }

        private void buttonFold_Click(object sender, EventArgs e)
        {
            FoldButtonPressed?.Invoke(sender, e);
        }

        private void buttonCheckCall_Click(object sender, EventArgs e)
        {
            CallButtonPressed?.Invoke(sender, e);
        }

        private void buttonBetRaise_Click(object sender, EventArgs e)
        {
            RaiseButtonPressed?.Invoke(sender, _trackBarRaiseAmm.Value);
        }

        private void trackBarRaiseAmm_Scroll(object sender, EventArgs e)
        {
            buttonBetRaise.Text = _raiseText + moneyToString(_trackBarRaiseAmm.Value);
        }
    }
}
