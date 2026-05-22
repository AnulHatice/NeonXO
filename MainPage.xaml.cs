using Microsoft.Maui.Devices;

namespace NeonXO;

public partial class MainPage : ContentPage
{
    bool isGameActive = false;
    bool isVsBot = true;
    bool isXNext = true;
    int scoreX = 0;
    int scoreO = 0;
    string p1Name = "";
    string p2Name = "";

    public MainPage()
    {
        InitializeComponent();
        ShowSplashScreen();
    }

    private async void ShowSplashScreen()
    {
        LoadingView.IsVisible = true;
        await LoadingLogo.ScaleTo(1.2, 800);
        await LoadingLogo.ScaleTo(1.0, 400);
        await Task.Delay(800);
        await LoadingView.FadeTo(0, 400);
        LoadingView.IsVisible = false;
        EntryView.IsVisible = true;
    }

    // Mod Seçimi Kontrolleri
    private void SelectBotMode(object sender, EventArgs e)
    {
        isVsBot = true;
        BotModeBtn.BackgroundColor = Color.FromArgb("#00FFFF");
        BotModeBtn.TextColor = Colors.Black;
        FriendModeBtn.BackgroundColor = Color.FromArgb("#1E1E1E");
        FriendModeBtn.TextColor = Colors.White;
        Player2Entry.Text = "Bot";
        Player2Entry.IsReadOnly = true;
    }

    private void SelectFriendMode(object sender, EventArgs e)
    {
        isVsBot = false;
        FriendModeBtn.BackgroundColor = Color.FromArgb("#FF00FF");
        FriendModeBtn.TextColor = Colors.White;
        BotModeBtn.BackgroundColor = Color.FromArgb("#1E1E1E");
        BotModeBtn.TextColor = Colors.White;
        Player2Entry.Text = "";
        Player2Entry.IsReadOnly = false;
    }

    private async void HandleStartGame(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Player1Entry.Text) || string.IsNullOrWhiteSpace(Player2Entry.Text))
        {
            await DisplayAlert("Eksik Bilgi", "Lütfen oyuncu isimlerini girin!", "Tamam");
            return;
        }

        p1Name = Player1Entry.Text;
        p2Name = Player2Entry.Text;

        EntryView.IsVisible = false;
        GameView.IsVisible = true;
        scoreX = 0; scoreO = 0;
        UpdateScoreLabels();
        ResetBoard();
        isGameActive = true;
    }

    private void BackToMenu(object sender, EventArgs e)
    {
        GameView.IsVisible = false;
        EntryView.IsVisible = true;
        isGameActive = false;
    }

    private async void OnButtonClicked(object sender, EventArgs e)
    {
        if (!isGameActive) return;
        var button = (Button)sender;
        if (!string.IsNullOrEmpty(button.Text)) return;

        await PlayMove(button, isXNext ? "X" : "O");
        if (CheckForWinner()) return;

        isXNext = !isXNext;
        UpdateTurnLabel();

        if (isVsBot && !isXNext)
        {
            isGameActive = false;
            await Task.Delay(600);
            await BotMove();
            isGameActive = true;
        }
    }

    private async Task BotMove()
    {
        var buttons = GameGrid.Children.Cast<Button>().ToList();
        var move = GetSmartMove(buttons);
        if (move == null)
        {
            var empty = buttons.Where(b => string.IsNullOrEmpty(b.Text)).ToList();
            if (empty.Count > 0) move = empty[new Random().Next(empty.Count)];
        }
        if (move != null)
        {
            await PlayMove(move, "O");
            if (!CheckForWinner()) { isXNext = true; UpdateTurnLabel(); }
        }
    }

    private async Task PlayMove(Button btn, string marker)
    {
        await btn.ScaleTo(0.85, 50); await btn.ScaleTo(1.0, 50);
        btn.Text = marker;
        btn.TextColor = marker == "X" ? Color.FromArgb("#FF00FF") : Color.FromArgb("#00FFFF");
        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(50)); } catch { }
    }

    private void UpdateTurnLabel() => TurnLabel.Text = $"Sıra: {(isXNext ? p1Name : p2Name)}";

    private void UpdateScoreLabels() { ScoreX.Text = $"{p1Name}: {scoreX}"; ScoreO.Text = $"{p2Name}: {scoreO}"; }

    private Button GetSmartMove(List<Button> buttons)
    {
        int[][] winners = { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, new[] { 0, 4, 8 }, new[] { 2, 4, 6 } };
        foreach (var combo in winners)
        {
            var line = combo.Select(i => buttons[i]).ToList();
            if (line.Count(b => b.Text == "O") == 2 && line.Count(b => string.IsNullOrEmpty(b.Text)) == 1) return line.First(b => string.IsNullOrEmpty(b.Text));
        }
        foreach (var combo in winners)
        {
            var line = combo.Select(i => buttons[i]).ToList();
            if (line.Count(b => b.Text == "X") == 2 && line.Count(b => string.IsNullOrEmpty(b.Text)) == 1) return line.First(b => string.IsNullOrEmpty(b.Text));
        }
        return null;
    }

    private bool CheckForWinner()
    {
        var buttons = GameGrid.Children.Cast<Button>().ToList();
        int[][] winners = { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, new[] { 0, 4, 8 }, new[] { 2, 4, 6 } };
        foreach (var combo in winners)
        {
            if (!string.IsNullOrEmpty(buttons[combo[0]].Text) && buttons[combo[0]].Text == buttons[combo[1]].Text && buttons[combo[1]].Text == buttons[combo[2]].Text)
            {
                DeclareWinner(buttons[combo[0]].Text); return true;
            }
        }
        if (buttons.All(b => !string.IsNullOrEmpty(b.Text))) { ShowPopup("Berabere!", "Dostluk kazandı."); return true; }
        return false;
    }

    private void DeclareWinner(string winner)
    {
        isGameActive = false;
        if (winner == "X") scoreX++; else scoreO++;
        UpdateScoreLabels();
        ShowPopup("Oyun Bitti", $"Kazanan: {(winner == "X" ? p1Name : p2Name)}");
    }

    private async void ShowPopup(string title, string msg)
    {
        PopupTitle.Text = title; PopupMessage.Text = msg; CustomPopup.IsVisible = true; CustomPopup.Opacity = 0;
        await CustomPopup.FadeTo(1, 300);
    }

    private async void ClosePopupClicked(object sender, EventArgs e)
    {
        await CustomPopup.FadeTo(0, 200); CustomPopup.IsVisible = false; ResetBoard(); isGameActive = true;
    }

    private void ResetBoard() { foreach (var child in GameGrid.Children.Cast<Button>()) child.Text = ""; isXNext = true; UpdateTurnLabel(); }
}