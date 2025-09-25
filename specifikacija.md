Implementirati SCADA sistem koji podržava sledeće funkcionalnosti: - - - - - 
dodavanje i uklanjanje analognih i digitalnih veličina (tags) sa sledećim 
osobinama: 
 Tip taga (enumeracija: DI, DO, AI ili AO) 
 Tag name (id) 
 Description 
 I/O addres 
 Scan time (moguće uneti samo za input tagove) 
 On/off scan (moguće uneti samo za input tagove) 
 Low limit (moguće uneti samo za analogne tagove) 
 High Limit (moguće uneti samo za analogne tagove) 
 Units (moguće uneti samo za analogne tagove) 
 Initial value (moguće uneti samo za output tagove) 
 Alarms (ne unosi se prilikom pravljenja taga nego se prilikom 
pravljenja alarma on veže za određeni AI) 
Sve zajedničke karakteristike tagova neka budu posebno bolje. Ostale 
karakteristike smestiti u rečnik. 
Izvršiti validaciju unesenih vrednosti i onemogućiti korisnika da unese 
neadekvatne podatke (npr. ne može se uneti units za digitalne tagove). 
dodavanje i uklanjanje alarma nad ulaznim analognim veličinama sa 
sledećim osobinama: 
 vrednost granice veličine, 
 da li se alarm aktivira kada vrednost veličine pređe iznad ili ispod 
vrednosti granice, 
 poruku o alarmu. 
pisanje vrednosti u digitalnu ili analognu izlaznu veličinu 
uključivanje i isključivanje skeniranja ulaznih tagova (on/off scan). 
čuvanje/iščitavanje konfiguracije (veličine i alarmi nad veličinama) u/iz 
baze podataka pri zatvaranju/pokretanju SCADA aplikacije. Potrebno je 
napraviti 3 tabele u bazi podataka: tagovi, alarmi i aktivirani alarmi. 
Scada WPF app predstavlja grafički interfejs pomoću kojeg korisnik može da 
doda/ukloni veličine, doda/ukloni alarme nad veličinama, upiše vrednost u 
određenu veličinu i služi za prikaz najnovijih informacija o promeni vrednosti 
veličina i informacija o najnovijim alarmima koji su se desili u sistemu. 
Data Concentrator predstavlja softversku komponentu koja sadrži sve 
trenutne vrednosti veličina i sve informacije o veličinama i alarmima. Data 
Concentrator na svaku promenu vrednosti veličine ispisuje da li je veličina u 
alarmnoj zoni. Ako jeste, izvršavaju se sledeći koraci: 
U bazu podataka upisuju se informacije o alarmu koji se desio: 
id alarma, 
naziv veličine nad kojom se desio alarm, 
poruka o alarmu, 
vreme alarma. 
Aktivira se događaj da se alarm sa datim ID-em desio. Data Concentrator je 
publisher, a Scada WPF app je subscriber. 
Scada WPF app obradi događaj tako što pročita iz baze podataka vrednost 
alarma sa datim ID-em i prikaže informacije na korisničkom interfejsu. 
Dodatno, na grafičkom interfejsu se nalazi dugme REPORT koje, kada kliknemo 
na njega, generiše .txt fajl u kom se nalaze podaci o vrednostima analognih ulaza 
kada su imali vrednost ℎ𝑖𝑔ℎ_𝑙𝑖𝑚𝑖𝑡 + 𝑙𝑜𝑤_𝑙𝑖𝑚𝑖𝑡
 2
 ±5.   
PLC Simulator predstavlja izvor vrednosti veličina koje se simuliraju. Veličine 
koje se defi- nišu u Scada WPF komponenti preko atributa I/O address 
mapiraju se na određenu vrednost iz kolekcije u PLC Simulator-u. Potrebno je