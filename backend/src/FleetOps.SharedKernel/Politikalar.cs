// Politika ADLARI SharedKernel'de: uc noktalar modullerde tanimli ama
// politikalarin ICERIGI composition root'ta. Moduller Api'yi referans
// veremeyecegi icin ortak olan ad burada durmali. Is kurali icermiyor.
namespace FleetOps.SharedKernel;

// Endpoint'ler role degil POLITIKAYA baglaniyor. Rol seti degisirse tek yer
// duzeltilir; uc noktalarda "Supervisor" string'i aranmaz.
public static class Politikalar
{
    // Okuma: giris yapmis herkes.
    public const string Okuma = "okuma";

    // Gorevi kim yurutur: sahadaki operator de, supervisor de.
    public const string GorevYurutme = "gorev-yurutme";

    // Gorev acmak ve AGV'ye atamak planlama isi.
    public const string GorevPlanlama = "gorev-planlama";

    // Telemetri araclarin kendi kimligiyle gelir.
    public const string Telemetri = "telemetri";
}
