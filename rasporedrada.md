## Raspored rada i podela zadataka

Ovaj dokument definiše podelu posla za dvoje, milestone‑e, očekivane isporuke i zajedničke interfejse između backend‑a i GUI‑ja.

## Trenutno stanje
- **Implementirano**: funkcionalan `PLCSimulatorManager` (generisanje AI/DI, setovanje AO/DO)
- **Nedostaje**: modeli i logika u `DataConcentrator` (`Tag`, `Alarm`, `ActivatedAlarm`, `ContextClass`), WPF GUI (`MainWindow`) je skeleton

## Podela posla

### Osoba A — Backend (DataConcentrator + Simulator proširenja)
- **Modeli i konfiguracija**
  - Definisati `Tag` hijerarhiju: AI, AO, DI, DO (adresa, opis, sken period, inženjerske granice, poslednja vrednost, kvalitet)
  - `Alarm`: tip (HI, HIHI, LO, LOLO), prag, prioritet, histereza
  - `ActivatedAlarm`: vreme aktivacije/deaktivacije, acknowledged, komentar
  - `ContextClass`: minimalna perzistencija (SQLite/EF Core ili JSON fajl) za tagove, istoriju merenja i istoriju alarma
- **Driver i scan engine**
  - Interfejs `IPlcDriver` koji wrap‑uje `PLCSimulatorManager` (Get/Set analog/digital)
  - Scan engine (timer/background task) koji periodično čita AI/DI i prosleđuje promene
  - Validacije adresa i jedinstvenosti tagova
- **Alarm engine**
  - Detekcija uslova sa histerezom i debouncing‑om
  - Generisanje `ActivatedAlarm`, ack/shelve mehanizam
  - Logovanje u istoriju i event‑ovi za GUI
- **Historian**
  - Upis uzoraka (npr. 1–5 s) i API za čitanje trendova (interval, downsampling)
- **Simulator poboljšanja**
  - Dodati još DI/DO adresa (po 4–8)
  - Zameniti `Thread.Abort()` cancellation token‑ima; zadržati thread‑safe pristup
- **Testovi**
  - Jedinični testovi za alarm logiku, histerezu i validaciju tagova

**Isporuke (A)**
- Kompletni modeli i konfiguracija u `DataConcentrator`
- Driver, scan i alarm engine, historian
- Serijalizacija konfiguracije (Load/Save)
- In‑proc API i event‑ovi koje GUI koristi

### Osoba B — GUI (WPF ScadaGUI, MVVM)
- **Arhitektura**
  - MVVM slojevi: ViewModels (Tagovi, Alarmi, Trend, Komande), Services (referenca na `DataConcentrator`), RelayCommand
- **Ekrani**
  - Tag Management: lista, dodaj/izmeni/obriši, validacije, bindovanje
  - Realtime pregled: tabela AI/DI (bojenje kvaliteta, filteri)
  - Komande: postavljanje AO/DO sa potvrdom akcije
  - Alarmi: Active Alarms (boje po prioritetu, ack, shelve), Alarm History sa filtrima
  - Trendovi: izbor tagova, vremenski opseg, auto‑refresh (bez spoljašnjih biblioteka ili minimalno)
- **Integracija**
  - Subscribovanje na evente iz `DataConcentrator` (promena vrednosti / nov alarm)
  - Load/Save konfiguracije preko backend servisa
  - Status bar: status skenera, broj aktivnih alarma, globalne greške
- **Testovi**
  - ViewModel testovi (komande, validacije) uz mock servis

**Isporuke (B)**
- Funkcionalni WPF UI sa MVVM i integracijom na backend
- Ekrani za tagove, alarme (aktivni + istorija), trendove i komande

## Zajednički interfejsi i ugovori
- **DTO/kontrakti** (dogovoriti pre rada): `TagDTO`, `AlarmDTO`, `ActivatedAlarmDTO`, `SampleDTO`
- **Event‑ovi** koje backend emituje:
  - `ValueChanged(tagId, value, quality, timestamp)`
  - `AlarmChanged(activatedAlarmDTO)`
- **Konfiguracija**: backend obezbeđuje serijalizaciju (JSON/SQLite), GUI poziva Load/Save

## Milestone‑i (okvirno)
- **M1 (2–3 dana)**
  - Dogovor DTO‑a i event interfejsa
  - Skeleton MVVM u GUI‑ju
  - Scan engine čita AI/DI iz simulatora i emituje `ValueChanged`
- **M2 (3–4 dana)**
  - CRUD tagova i validacije u GUI‑ju
  - Osnovna alarm detekcija u backend‑u i prikaz Active Alarms
- **M3 (3–4 dana)**
  - Historian + Trend ekran
  - Ack/shelve + Alarm History u GUI‑ju
  - Komande AO/DO iz GUI‑ja
- **M4 (2 dana)**
  - Stabilizacija, testovi, dokumentacija i demo scenario

## Kriterijumi prihvatanja
- Stabilan scan bez blokiranja UI‑ja; bez `Thread.Abort()` u produkcionom kodu
- Alarm histereza i ack/shelve funkcionišu i beleže se u istoriji
- Trend prikazuje više tagova uz izbor perioda i basic downsampling
- Komande AO/DO imaju potvrdu i audit log
- Konfiguracija se pouzdano učitava/čuva; validacije sprečavaju nevažeće adrese

## Napomene
- Proširiti simulator dodatnim DI/DO adresama (npr. `ADDR011`–`ADDR016`)
- Dogovoriti ID‑eve tagova (GUID/string) i format timestamp‑a (UTC)

