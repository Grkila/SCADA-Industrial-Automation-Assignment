
1
Entity Framework 
Relacione baze podataka 
Relaciona baza podataka je poseban tip baze podataka kod kojeg se organizacija podataka zasniva 
na relacionom modelu. Podaci se u ovakvim bazama organizuju u skup relacija između kojih se 
definišu određene veze. Relacija se definiše kao skup n-torki sa istim atributima, definisanih nad 
istim domenima iz kojih mogu da uzimaju vrednosti. U relacionim bazama podataka, svaka relacija 
mora  da  ima  definisan  primarni ključ,  koji  predstavlja atribut pomoću kojeg se jedinstveno 
identifikuje svaka n-torka. Relacija opciono može da poseduje i strani ključ, preko kojeg ostvaruje 
vezu sa drugim relacijama. Svaka relacija može se predstaviti u tabelarnom obliku, pa se ti termini 
često koriste kao sinonimi (iako strogo gledano to nisu). Naredna slika prikazuje jednostavnu bazu 
podataka sa dve relacije: knjige i pisci. 
Books 
Id Title Year Rating AuthorId 
1 Za kim zvona zvone 1940 3.97 1 
2 Plodovi gneva 1939 3.96 2 
3 Istočno od raja 1952 4.37 2 
4 2666 2004 4.21 3 
5 Divlji detektivi 1998 4.12 3 
6 Kvaka 22 1961 3.98 4 
7 Hari Poter 1-7 2007 4.74 5 
8 Autostoperski vodič kroz galaksiju 1996 4.37 6 
9 Travnička hronika 1945 4.26 7 
10 Znakovi pored puta 1976 4.47 7 
Authors 
Id Name Gender Born Died 
1 Ernest Hemingway Male 21/07/1899 02/07/1961 
2 John Steinbeck Male 27/02/1902 20/12/1968 
3 Roberto Bolano Male 28/04/1953 15/07/2003 
4 Joseph Heller Male 01/05/1923 12/12/1999 
5 J.K. Rowling Female 31/07/1965  
6 Douglas Adams Male 11/03/1952 11/05/2001 
7 Ivo Andric Male 09/10/1892 13/03/1975 
Primarni ključevi ovih tabela su atributi Id, jer su oni jedinstveni za svaku n-torku relacija. Obično 
je praksa da se za primarni ključ kreira novi, numerički atribut koji će biti jedinstven za svaku n-
torku date relacije (nije pravilo, može se koristiti i neki postojeći atribut ukoliko je „prirodno“ da 
bude ključ – npr. JMBG, bar-kod i slično). Tabela Books sadrži i strani ključ kojim je povezana 
sa tabelom Authors – AuthorId. Strani ključ je, kao što mu i ime kaže, atribut neke relacije dobijen 
„prenošenjem“ primarnog ključa iz druge relacije.  U ovom slučaju, strani ključ AuthorId je 
posledica 1-N veze (jedan na više) između knjiga i autora: svaku knjigu je napisao jedan autor, 
dok autor može da napiše više knjiga.  Bitno ograničenje jeste da je domen vrednosti atributa 
AuthorId iz relacije Books podskup domena atributa Id iz relacije Authors. Drugim rečima, atribut 
AuthorId  može da sadrži samo vrednosti koje se već pojavljuju u  koloni  Id  tabele  Authors. 
Napomena: ovo je ograničenje na nivou baze, nije nešto što vi implementirate. 
Upravljanje  relacionim  bazama  podataka  realizuje  se  preko  sistema  za  upravljanje  bazama 
podataka (SUBP). Neki od najpopularnijih SUBP-ova su: Microsoft SQL Server, Oracle Database, 
MySQL, itd. 
Entity Framework 
Entity Framework je open-source ORM (Object-relational mapping) framework za .NET 
aplikacije. Entity Framework omogućava rad sa podacima korišćenjem objekata određenih klasa 
umesto fokusiranja na detalje baze podataka i tabela u kojima su ti podaci skladišteni. Ovim je 
eliminisana potreba za pisanjem koda zaduženog za sam pristup podacima. Sledeća slika ilustruje 
poziciju Entity Framework-a u tipičnoj aplikaciji. Može se primetiti da se Entity Framework nalazi 
između baze podataka i biznis logike (klase sistema), sa kojima je u dvosmernoj „komunikaciji“: 
čuva podatke skladištene u svojstvima (properties) klasa i dobavlja podatke iz baze i „prepakuje“ 
ih u objekte odgovarajućih klasa. 
 
Dva suštinski različita pristupa u korišćenju Entity  Framework-a  i  projektovanju  biznis  logike 
(klasa) neke aplikacije su: 
• Code First - implementacija klasa pa potom njihovo prevođenje u tabele baze podataka, 
• Database First – projektovanje baze podataka pa potom prevođenje u klase. 
Entity Framework podržava oba navedena načina, koji su detaljnije objašnjeni u nastavku. Da biste 
mogli da odradite sve što je prikazano u nastavku neophodno je da imate Visual Studio > 2010. Ko 
koristi VS 2010 mora da instalira i NuGet. 
Code First pristup 
Code First pristup omogućava realizaciju baze podataka kroz implementaciju klasa. Klase koje se 
implementiraju su gotovo identične „normalnim“ klasama, s tim da je potrebno izvršiti manje 
izmene da bismo maksimalno iskoristili Entity Framework. U nastavku je dat izgled klasa (samo 
svojstva) Book i Author. public class Author 
{ 
 [Key]             
 public int Id { get; set; }   
 public string Name { get; set; } 
 public Gender Gender { get; set; } 
 public DateTime Born { get; set; } 
 public DateTime? Died { get; set; } 
 
 public virtual List<Book> Books { get; set; } 
} 
 public class Book 
{ 
 [Key] 
 public int Id { get; set; }  
 public string Title { get; set; } 
 public int Year { get; set; } 
 public double Rating { get; set; } 
 
 public int AuthorId { get; set; } 
 public virtual Author Author { get; set; } 
} 
 
Prva  izmena  u  odnosu  na  do  sada  implementirane  klase  je  upotreba  anotacija.  Anotacije  nisu 
isključivo vezane za Entity  Framework  i postoji znatno veći broj različitih anotacija i njihovih 
upotreba u .NET-u. Ovde je korišćena jedino Key anotacija, koja govori Entity Framework-u da 
od svojstva Id želimo da napravimo primarni ključ (za obe tabele, naravno ne mora da se zove Id). 
Bitno je napomenuti da postoji i anotacija ForeignKey, koja se koristi da se naglasi da je svojstvo 
strani ključ, dobijen prenošenjem primarnog ključa iz neke druge tabele. U prethodnom primeru 
anotacija  ForeignKey  nije  korišćena  jer  je  svojstvo  koje  predstavlja  strani  ključ  nazvano  u 
adekvatnom  formatu  (AuthorId).  U  slučaju  da  želimo  drugačije  da  nazovemo  strani  ključ, 
neophodno je korišćenje ForeignKey anotacije u kojoj će se navesti ime tabele iz koje se prenosi 
ključ, iznad svojstva koje predstavlja strani ključ. 
Druga  bitna  izmena  jeste  upotreba  virtuelnih  svojstava  prilikom  kompozicije  (virtual  Author  i 
virtual List<Book>).  Ovim  se  omogućava  jedna  izuzetno  korisna  funkcionalnost  Entity 
Framework-a nazvana Lazy Loading. Uloga Lazy Loading-a je automatsko učitavanje određenih 
podataka iz baze tek u trenutku prvog pristupa svojstvima. U ovom slučaju to znači da će se lista 
knjiga za svakog autora popuniti automatski iz tabele knjiga tek prilikom prvom pristupu  Books 
svojstvu. Ovim se takođe smanjuje potreba za eksplicitnim spajanjem tabela (join), jer je moguće 
automatsko učitavanje (i dalje je ponekad neophodno). 
Entity Framework prevodi klase Author i Book u adekvatan SQL kod, pomoću kog se kreiraju dve 
tabele u bazi. U nastavku je dat izgled SQL koda kojim su kreirane tabele Authors i Books. 
