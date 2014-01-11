using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace NumberPuzzle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
   
    public partial class MainWindow : Window
    {
        private Board logicalBoard;
        private string saveFileName = "NumberPuzzle.dat";
        //add fields for menu operations here

        public MainWindow()
        {
            InitializeComponent();
            logicalBoard = new Board(saveFileName);
            logicalBoard.OnBoardChanged += updateView;
            //force update
            updateView(logicalBoard, new BoardChangedEventArgs(null, null, true));
        }

        //mix/unmix board
        private void MixButton_Click(object sender, RoutedEventArgs e)
        {
            logicalBoard.mixPuzzle();
        }

        //grow/shrink board
        private void ShrinkBoard_Click(object sender, RoutedEventArgs e)
        {
            newLogicalBoard(logicalBoard.Width - 1);
        }
        private void GrowBoard_Click(object sender, RoutedEventArgs e)
        {
            newLogicalBoard(logicalBoard.Width + 1);
        }

        //exit game
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show("Save Game?", "Save Current Game", MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question, MessageBoxResult.No);
            if (result == MessageBoxResult.Cancel) e.Cancel = true;
            else if (result == MessageBoxResult.Yes)
            {
                logicalBoard.save(saveFileName, SaveOption.saveSettingsAndBoard);
            }
            else //don't save: save settings only
            {
                logicalBoard.save(saveFileName, SaveOption.saveSettingsOnly);
            }
        }

        //drag board
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        //Drag/drop buttons (fields)
        private bool dragging = false;
        private Button dragee = null;
        private Point dragOrigin;
        private Direction allowedDragDirection; //based on logical board
        const int dragDelta = 5; //use Windows constant when I figure out how

        //drag/drop buttons (event handlers)
        private void startDrag(object sender, MouseButtonEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                Tile tile = button.Content as Tile;
                if (tile != null)
                {
                    //determine if drag is allowed and which direction
                    if (tile.canMove(Direction.down))
                    { this.allowedDragDirection = Direction.down; }
                    else if (tile.canMove(Direction.up))
                    { this.allowedDragDirection = Direction.up; }
                    else if (tile.canMove(Direction.left))
                    { this.allowedDragDirection = Direction.left; }
                    else if (tile.canMove(Direction.right))
                    { this.allowedDragDirection = Direction.right; }
                    else return;
                    //if drag allowed, set drag variables
                    this.dragging = true;
                    this.dragee = button;
                    this.dragOrigin = e.GetPosition(Application.Current.MainWindow);
                }
            }
        }
        private void currentlyDragging(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point currentPosition = e.GetPosition(Application.Current.MainWindow);
                double deltaX = currentPosition.X - this.dragOrigin.X;
                double deltaY = currentPosition.Y - this.dragOrigin.Y;
                double maxDeltaX = this.dragee.ActualWidth;
                double maxDeltaY = this.dragee.ActualHeight;
                //render transform button to follow mouse
                //only drags in allowed direction and not further than final resting place
                switch (this.allowedDragDirection)
                {
                    case Direction.up:
                        deltaX = 0;
                        deltaY = deltaY <= -MainWindow.dragDelta ? deltaY : 0;
                        deltaY = deltaY <= -maxDeltaY ? -maxDeltaY : deltaY;
                        break;
                    case Direction.down:
                        deltaX = 0;
                        deltaY = deltaY >= MainWindow.dragDelta ? deltaY : 0;
                        deltaY = deltaY >= maxDeltaY ? maxDeltaY : deltaY;
                        break;
                    case Direction.left:
                        deltaY = 0;
                        deltaX = deltaX <= -MainWindow.dragDelta ? deltaX : 0;
                        deltaX = deltaX <= -maxDeltaX ? -maxDeltaX : deltaX;
                        break;
                    case Direction.right:
                        deltaY = 0;
                        deltaX = deltaX >= MainWindow.dragDelta ? deltaX : 0;
                        deltaX = deltaX >= maxDeltaX ? maxDeltaX : deltaX;
                        break;
                    default: return;
                }
                dragee.RenderTransform = new TranslateTransform(deltaX, deltaY);
            }
        }
        private void endDrag(object sender, MouseButtonEventArgs e)
        {
            if (dragging)
            {
                Button button = sender as Button;
                if (button != null)
                {
                    Tile tile = button.Content as Tile;
                    if (tile != null)
                    {
                        //determine whether to drop button in new location or snap back
                        //if more than half-way, make switch
                        Point endPosition = e.GetPosition(Application.Current.MainWindow);
                        double deltaX = endPosition.X - this.dragOrigin.X;
                        double deltaY = endPosition.Y - this.dragOrigin.Y;
                        double threshholdDeltaX = this.dragee.ActualWidth / 2;
                        double threshholdDeltaY = this.dragee.ActualHeight / 2;
                        switch (this.allowedDragDirection)
                        {
                            case Direction.up:
                                if (deltaY <= -threshholdDeltaY)
                                    tile.move(Direction.up);
                                else button.RenderTransform = null;
                                break;
                            case Direction.down:
                                if (deltaY >= threshholdDeltaY)
                                    tile.move(Direction.down);
                                else button.RenderTransform = null;
                                break;
                            case Direction.left:
                                if (deltaX <= -threshholdDeltaX)
                                    tile.move(Direction.left);
                                else button.RenderTransform = null;
                                break;
                            case Direction.right:
                                if (deltaX >= threshholdDeltaX)
                                    tile.move(Direction.right);
                                else button.RenderTransform = null;
                                break;
                            default: return;
                        }
                    }
                }
            }
            this.dragging = false;
            this.dragee = null;
        }
        
        //helper methods

        //updates view
        private void updateView(object sender, BoardChangedEventArgs e)
        {
            if (e.newBoard) resetView();
            else swapTiles(e.tile1, e.tile2);
        }
        //used to update view
        private void swapTiles(Tile tile1, Tile tile2)
        {
            if (tile1 != null && tile2 != null)
            {
                Button b1 = null, b2 = null;
                //find buttons containing tiles
                foreach (object o in grid.Children)
                {
                    Button b = o as Button;
                    if (b != null)
                    {
                        if (b.Content == tile1) b1 = b;
                        else if (b.Content == tile2) b2 = b;
                    }
                }
                //switch button content, color, visibility
                if (b1 != null && b2 != null)
                {
                    //switch tile content
                    b1.Content = tile2;
                    b2.Content = tile1;
                    //switch button color if relevant
                    Brush temp = b1.Background;
                    b1.Background = b2.Background;
                    b2.Background = temp;
                    //switch visibility
                    System.Windows.Visibility temp2 = b1.Visibility;
                    b1.Visibility = b2.Visibility;
                    b2.Visibility = temp2;
                    //negate drag & drop render transform
                    b1.RenderTransform = null;
                    b2.RenderTransform = null;
                }
            }
        }
        //used to update view: re-creates view from logical board
        private void resetView()
        {
            grid.Children.Clear();

            int fontSize = logicalBoard.Height > 4 ? 66 - (logicalBoard.Height - 4) * 7 :
                           logicalBoard.Height < 4 ? 66 + (4 - logicalBoard.Height) * 10 :
                           66;

            for (int i = 0; i < logicalBoard.Squares; ++i)
            {
                Tile t = logicalBoard[i];
                Button b = new Button();
                b.Content = t;
                b.Foreground = new SolidColorBrush(Colors.Goldenrod);
                //crimson/white checkered color scheme
                int value = t.Value;
                b.Background =
                    //in board with even width
                    ((logicalBoard.Width % 2 == 0 &&
                    //odd-valued tiles in even starting rows or even-valued tiles in odd starting rows = white
                    ((value % 2 != 0 && (value - 1) / logicalBoard.Width % 2 == 0)
                        || (value % 2 == 0 && (value - 1) / logicalBoard.Width % 2 != 0)))
                    //board with odd width, odd-valued tiles = white
                    || (logicalBoard.Width % 2 != 0 && value % 2 != 0)) ?
                    new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Crimson);
                b.FontSize = fontSize;
                b.FontWeight = FontWeights.Heavy;
                b.PreviewMouseLeftButtonDown += startDrag;
                b.PreviewMouseMove += currentlyDragging;
                b.PreviewMouseLeftButtonUp += endDrag;
                if (value == 0)
                {
                    b.Visibility = Visibility.Hidden;
                }
                grid.Children.Add(b);
            }
        }
        //used to grow/shrink board: re-creates logical board of new size
        private void newLogicalBoard(int size = Board.DEFAULT_WIDTH)
        {
            logicalBoard = new Board(size, size);
            logicalBoard.OnBoardChanged += updateView;
            logicalBoard.unmixPuzzle();
        }
    }
}
