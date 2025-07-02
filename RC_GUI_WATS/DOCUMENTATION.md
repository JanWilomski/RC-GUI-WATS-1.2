# Dokumentacja Aplikacji RC GUI WATS

## 1. Wprowadzenie i Cel Aplikacji

**RC GUI WATS** to aplikacja desktopowa napisana w technologii WPF (Windows Presentation Foundation), która służy jako interfejs graficzny (GUI) dla systemu "Risk Checker" (RC). Jej głównym celem jest monitorowanie w czasie rzeczywistym oraz zarządzanie ryzykiem w systemie transakcyjnym.

Aplikacja umożliwia użytkownikom:
*   **Monitorowanie połączenia** z serwerem RC.
*   **Śledzenie na żywo wiadomości** giełdowych (CCG) i komunikatów systemowych.
*   **Podgląd aktualnych pozycji** na instrumentach finansowych.
*   **Monitorowanie stanu kapitału** i jego wykorzystania w stosunku do nałożonych limitów.
*   **Przeglądanie, dodawanie i modyfikowanie limitów ryzyka** (np. na maksymalną wartość transakcji, maksymalną pozycję).
*   **Przeglądanie definicji instrumentów** finansowych.

## 2. Architektura (Wzorzec MVVM)

Aplikacja jest zbudowana w oparciu o wzorzec projektowy **MVVM (Model-View-ViewModel)**, co zapewnia doskonałą separację odpowiedzialności, ułatwia testowanie i dalszy rozwój.


### 2.1. Model
Warstwa modelu definiuje obiekty biznesowe i struktury danych aplikacji. Są to proste klasy (POCO), które nie zawierają żadnej logiki biznesowej.

*   **Lokalizacja:** Katalog `Models/`
*   **Kluczowe klasy:**
    *   `RcMessage`, `RcMessageBlock`: Reprezentują strukturę protokołu komunikacyjnego z serwerem RC.
    *   `CcgMessage`: Reprezentuje pojedynczą wiadomość systemu transakcyjnego (np. zlecenie, transakcja).
    *   `ControlLimit`: Reprezentuje pojedynczy limit ryzyka.
    *   `Position`: Reprezentuje pozycję na danym instrumencie.
    *   `Instrument`: Zawiera pełne dane o instrumencie finansowym.
    *   `OrderBookEntry`: Reprezentuje pojedyncze zlecenie w księdze zleceń.

### 2.2. View
Warstwa widoku jest odpowiedzialna za interfejs użytkownika (UI). Składa się z plików XAML, które w sposób deklaratywny definiują wygląd i układ kontrolek. Widoki są "głupie" - nie zawierają logiki aplikacji, a jedynie bindowania do właściwości i komend w ViewModelu.

*   **Lokalizacja:** Katalog `Views/` oraz pliki `*Window.xaml`.
*   **Kluczowe pliki:**
    *   `MainWindow.xaml`: Główne okno aplikacji z zakładkami.
    *   `MessagesTabControl.xaml`: Widok zakładki "Messages".
    *   `SettingsTabControl.xaml`: Widok zakładki "Settings".
    *   `InstrumentsTabControl.xaml`: Widok zakładki "Instruments".

### 2.3. ViewModel
Warstwa ViewModel działa jak pośrednik między Modelem a Widokiem. Zawiera całą logikę prezentacji i stan interfejsu użytkownika. Udostępnia dane z Modelu w formie właściwości, do których Widok może się bindować, oraz komendy (`ICommand`), które Widok może wywoływać w odpowiedzi na akcje użytkownika.

*   **Lokalizacja:** Katalog `ViewModels/`
*   **Kluczowe klasy:**
    *   `BaseViewModel`: Klasa bazowa implementująca `INotifyPropertyChanged` w celu automatycznej aktualizacji UI.
    *   `MainWindowViewModel`: "Mózg" całej aplikacji. Inicjalizuje inne ViewModele, zarządza stanem połączenia i koordynuje przepływ danych.
    *   `MessagesTabViewModel`: Logika dla zakładki "Messages", w tym filtrowanie wiadomości i statystyki.
    *   `SettingsTabViewModel`: Logika dla zakładki "Settings", w tym zarządzanie limitami i ustawieniami.

### 2.4. Services
Usługi (Services) to klasy, które hermetyzują logikę niezwiązaną bezpośrednio z UI. Odpowiadają za komunikację z serwerem, parsowanie danych, logowanie, dostęp do plików itp. Są one wstrzykiwane do ViewModeli, co promuje luźne powiązania i ułatwia testowanie.

*   **Lokalizacja:** Katalog `Services/`
*   **Kluczowe klasy:**
    *   `RcTcpClientService`: Zarządza połączeniem TCP z serwerem RC.
    *   `CcgMessagesService`: Przetwarza przychodzące wiadomości CCG.
    *   `OrderBookService`: Buduje i zarządza wirtualną księgą zleceń.
    *   `LimitsService`: Zarządza kolekcją limitów.
    *   `ConfigurationService`: Odczytuje i zapisuje ustawienia z pliku `app.config`.
    *   `FileLoggingService`: Odpowiada za zapisywanie logów do pliku.

### 2.5. Wstrzykiwanie Zależności (Dependency Injection)
Aplikacja wykorzystuje prosty mechanizm wstrzykiwania zależności konfigurowany w pliku `App.xaml.cs`. Przy starcie aplikacji tworzone są instancje wszystkich usług, a następnie wstrzykiwane do konstruktora `MainWindowViewModel`.

## 3. Kinematyka - Główne Przepływy Danych i Zdarzeń

Ta sekcja opisuje, jak poszczególne komponenty współpracują ze sobą w kluczowych scenariuszach.

### Scenariusz 1: Uruchomienie Aplikacji i Połączenie z Serwerem

1.  **Start Aplikacji:** `App.xaml.cs` w metodzie `OnStartup` wywołuje `ConfigureServices`.
2.  **Tworzenie Usług:** Tworzone są pojedyncze instancje wszystkich usług (`ConfigurationService`, `FileLoggingService`, `RcTcpClientService`, `CcgMessagesService` itd.).
3.  **Tworzenie Głównego ViewModelu:** Tworzony jest `MainWindowViewModel`, a instancje usług są wstrzykiwane do jego konstruktora.
4.  **Inicjalizacja:** `MainWindowViewModel` w swoim konstruktorze:
    *   Subskrybuje kluczowe zdarzenia, np. `_clientService.ConnectionStatusChanged`.
    *   Inicjalizuje ViewModele dla poszczególnych zakładek (np. `MessagesTabViewModel`).
    *   Sprawdza w `ConfigurationService`, czy włączone jest automatyczne połączenie (`AutoConnect`).
5.  **Nawiązywanie Połączenia:**
    *   Jeśli `AutoConnect` jest `true` (lub użytkownik kliknie "Połącz"), wywoływana jest metoda `ConnectToServerAsync` w `MainWindowViewModel`.
    *   Metoda ta wywołuje `_clientService.ConnectAsync()`.
    *   `RcTcpClientService` deleguje to zadanie do `RcTcpClient`, który nawiązuje fizyczne połączenie TCP.
6.  **Pętla Odbioru Danych:** Po udanym połączeniu, `RcTcpClient` uruchamia w osobnym wątku pętlę `ReceiveMessagesAsync`, która nasłuchuje na przychodzące dane.
7.  **Aktualizacja Stanu:** `RcTcpClient` po zmianie statusu połączenia wywołuje zdarzenie `ConnectionStatusChanged`. `MainWindowViewModel` odbiera je i aktualizuje właściwości UI (np. `IsConnected`, `ConnectionStatus`).
8.  **Pobieranie Danych Historycznych:** Po połączeniu, `ConnectToServerAsync` wysyła do serwera dwa żądania:
    *   **Rewind (`R`):** `_clientService.SendRewindAsync(0)` prosi serwer o ponowne wysłanie wszystkich wiadomości od początku.
    *   **Get Controls History (`G`):** `SettingsTab.LoadControlHistoryAsync()` prosi serwer o wysłanie wszystkich aktualnie obowiązujących limitów.

### Scenariusz 2: Otrzymanie i Przetworzenie Wiadomości z Serwera

1.  **Odbiór Danych:** Pętla `ReceiveMessagesAsync` w `RcTcpClient` odczytuje dane z gniazda sieciowego.
2.  **Parsowanie Protokołu RC:** Dane są parsowane do obiektu `RcMessage`, który zawiera nagłówek i listę bloków (`RcMessageBlock`).
3.  **Zdarzenie `MessageReceived`:** `RcTcpClient` wywołuje zdarzenie `MessageReceived`, przekazując zrekonstruowaną wiadomość.
4.  **Dystrybucja w `RcTcpClientService`:** `RcTcpClientService` odbiera to zdarzenie i przekazuje je dalej do wszystkich swoich subskrybentów.
5.  **Przetwarzanie przez Usługi:** Różne usługi nasłuchują na to zdarzenie i przetwarzają te bloki, które ich interesują:
    *   `CcgMessagesService`: Interesują go bloki typu `'B'` (CCG). Wyodrębnia binarne dane wiadomości CCG, parsuje je za pomocą `CcgMessageParser`, mapuje `InstrumentId` na ISIN (używając `InstrumentsService`) i dodaje obiekt `CcgMessage` do swojej kolekcji.
    *   `PositionsService`: Interesują go bloki typu `'P'` (Position). Parsuje dane i aktualizuje kolekcję pozycji.
    *   `CapitalService`: Interesują go bloki typu `'C'` (Capital) oraz logi (`'I'`), z których wyciąga informacje o globalnych limitach.
    *   `LimitsService`: Interesują go bloki typu `'S'` (Set Control) oraz logi (`'I'`), które zawierają historię lub potwierdzenia ustawienia limitów.
6.  **Aktualizacja Księgi Zleceń:**
    *   Gdy `CcgMessagesService` doda nową wiadomość, wywołuje własne zdarzenie `NewCcgMessageReceived`.
    *   `OrderBookService` nasłuchuje na to zdarzenie. Jeśli wiadomość dotyczy zlecenia (np. `OrderAdd`, `Trade`, `OrderCancel`), aktualizuje stan odpowiedniego zlecenia w swojej wewnętrznej kolekcji.
7.  **Aktualizacja UI:** Wszystkie główne kolekcje danych (w `CcgMessagesService`, `PositionsService`, `LimitsService`, `OrderBookService`) to `ObservableCollection`. Dzięki mechanizmowi bindowania danych w WPF, każda zmiana w tych kolekcjach jest **automatycznie** odzwierciedlana w interfejsie użytkownika (w `DataGrid`ach itp.) bez potrzeby pisania dodatkowego kodu.

### Scenariusz 3: Zmiana Limitu Ryzyka przez Użytkownika

1.  **Akcja Użytkownika:** Użytkownik w zakładce "Settings" (`SettingsTabControl`) wypełnia formularz "Szybka zmiana limitów" i klika przycisk "Zastosuj".
2.  **Wywołanie Komendy:** Przycisk jest zbindowany do `ApplyQuickLimitCommand` w `SettingsTabViewModel`.
3.  **Walidacja i Tworzenie Limitu:** `SettingsTabViewModel` waliduje dane wejściowe i tworzy z nich obiekt `ControlLimit`.
4.  **Wysłanie do Usługi:** ViewModel wywołuje metodę `_limitsService.SendControlLimitAsync(newLimit)`.
5.  **Wysłanie do Serwera:**
    *   `LimitsService` przekazuje żądanie do `_clientService.SendSetControlAsync(controlString)`.
    *   `RcTcpClientService` deleguje je do `_client.SendSetControlAsync(controlString)`.
    *   `RcTcpClient` konstruuje wiadomość `RcMessage` z blokiem typu `'S'` (Set Control) i wysyła ją przez gniazdo TCP do serwera RC.
6.  **Potwierdzenie od Serwera:** Serwer RC przetwarza żądanie i odsyła potwierdzenie (zazwyczaj w formie wiadomości logu typu `'I'`).
7.  **Aktualizacja UI:** Wiadomość z potwierdzeniem jest odbierana i przetwarzana zgodnie ze **Scenariuszem 2**. `LimitsService` przechwytuje ją, aktualizuje swoją kolekcję `ControlLimits`, co z kolei automatycznie odświeża tabelę z limitami w interfejsie użytkownika.

## 4. Podsumowanie i Wskazówki dla Programistów

*   **Solidne podstawy:** Aplikacja jest zbudowana na solidnych fundamentach architektonicznych (MVVM, DI), co czyni ją łatwą w utrzymaniu i rozbudowie.
*   **Separacja odpowiedzialności:** Każda klasa ma jasno zdefiniowaną odpowiedzialność, co ułatwia zrozumienie kodu.
*   **Reaktywność:** Dzięki zdarzeniom i bindowaniu danych, aplikacja jest reaktywna - dane przepływają od serwera, przez usługi, do UI w sposób zautomatyzowany.
*   **Potencjalne ulepszenia:**
    *   **Testy jednostkowe:** Architektura jest gotowa na testy. Można dodać projekty testowe i pisać testy dla ViewModeli i usług, mockując ich zależności.
    *   **Bardziej rozbudowana obsługa błędów:** Można dodać bardziej szczegółową obsługę błędów sieciowych i mechanizmy ponawiania połączenia.
    *   **Wydajność:** Przy bardzo dużej liczbie wiadomości warto zwrócić uwagę na wydajność `DataGrid`ów i rozważyć wirtualizację UI, jeśli nie jest jeszcze włączona.
