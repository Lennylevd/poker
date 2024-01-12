using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace Poker
{
    class Program
    {
        // -----------------------
        // DECLARATION DES DONNEES
        // -----------------------
        // Importation des DL (librairies de code) permettant de gérer les couleurs en mode console
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, int wAttributes);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetStdHandle(uint nStdHandle);
        static uint STD_OUTPUT_HANDLE = 0xfffffff5;
        static IntPtr hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
        // Pour utiliser la fonction C 'getchar()' : sasie d'un caractère
        [DllImport("msvcrt")]
        static extern int _getche();

        //-------------------
        // TYPES DE DONNEES
        //-------------------

        // Fin du jeu
        public static bool fin = false;

        // Codes COULEUR
        public enum couleur { VERT = 10, ROUGE = 12, JAUNE = 14, BLANC = 15, NOIRE = 0, ROUGESURBLANC = 252, NOIRESURBLANC = 240 }; //

        // Coordonnées pour l'affichage
        public struct coordonnees
        {
            public int x;
            public int y;
        }

        // Une carte
        public struct carte
        {
            public char valeur;
            public int famille;
        };
		
        public static Random rnd=new Random();
        // Liste des combinaisons possibles
        public enum combinaison { RIEN, PAIRE, DOUBLE_PAIRE, BRELAN, QUINTE, FULL, COULEUR, CARRE, QUINTE_FLUSH };

        // Valeurs des cartes : As, Roi,...
        public static char[] valeurs = { 'A', 'R', 'D', 'V', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

        // Codes ASCII (3 : coeur, 4 : carreau, 5 : trèfle, 6 : pique)
        public static int[] familles = { 3, 4, 5, 6 };

        // Numéros des cartes à échanger
        public static int[] echange = { 0, 0, 0, 0 };

        // Jeu de 5 cartes
        public static carte[] MonJeu = new carte[5];

        //----------
        // FONCTIONS
        //----------

        // Génère aléatoirement une carte : {valeur;famille}
        // Retourne une expression de type "structure carte"
        public static carte tirage()
        {
			// Génère aléat de la valeur et de la famille
   		 	int iValeur = rnd.Next(0, valeurs.Length);
   	 		int iFamille = rnd.Next(0, familles.Length);

    		// Nouv carte avec valeur et la famille
    		carte nouvCarte = new carte
    		{
        		valeur = valeurs[iValeur],
        		famille = familles[iFamille]
    		};

    		return nouvCarte;
        }

        // Indique si une carte est déjà présente dans le jeu
        // Paramètres : une carte, le jeu 5 cartes, le numéro de la carte dans le jeu
        // Retourne un entier (booléen)
        public static bool carteUnique(carte uneCarte, carte[] unJeu, int numero)
        {
		 	// Parcours du tab de jeu pour vérif si la carte est déjà présente
    		for (int i = 0; i < unJeu.Length; i++)
   				{
        		// Compare pas la carte avec elle-même (celle à la même position)
       			if (i != numero)
       				{
           			// Comparaison de la famille et de la valeur
            		if (uneCarte.famille == unJeu[i].famille && uneCarte.valeur == unJeu[i].valeur)
            			{
                		// La carte est déjà présente
                		return false;
            			}
        			}
    			}

    		// La carte est unique
    		return true;
        }

        // Calcule et retourne la COMBINAISON (PAIRE, DOUBLE_PAIRE... QUINTE_FLUSH)
        // pour un jeu complet de 5 cartes.
        // La valeur retournée est un élement de l'énumération 'combinaison' (=constante)
        public static combinaison chercheCombinaison(ref carte[] unJeu)
        {
			// Trie les cartes par valeur
    		Array.Sort(unJeu, (x, y) => x.valeur.CompareTo(y.valeur));

    		// Vérifie la quinte flush
    		bool quinteFlush = true;
    		for (int i = 1; i < unJeu.Length; i++)
    		{
        		if (unJeu[i].famille != unJeu[i - 1].famille || unJeu[i].valeur != unJeu[i - 1].valeur + 1)
        		{
            		quinteFlush = false;
            		break;
        		}
    		}

    		// Vérifie la couleur
    		bool couleur = unJeu[0].famille == unJeu[1].famille && unJeu[0].famille == unJeu[2].famille && unJeu[0].famille == unJeu[3].famille && unJeu[0].famille == unJeu[4].famille;

    		// Compte les paires, brelans et carrés
    		int paires = 0;
    		int brelans = 0;
    		int carres = 0;

    		// Compte le nombre de chaque valeur de carte
    		char lastValue = '\0';
    		int valueCount = 1;

    		for (int i = 1; i < unJeu.Length; i++)
    		{
        		if (unJeu[i].valeur == lastValue)
        		{
            		valueCount++;
        		}
        		else
        		{
            		if (valueCount == 2)
                		paires++;
            		else if (valueCount == 3)
                		brelans++;
            		else if (valueCount == 4)
                		carres++;

            		lastValue = unJeu[i].valeur;
            		valueCount = 1;
        		}
   		 	}

    		if (valueCount == 2)
       		 	paires++;
    		else if (valueCount == 3)
        		brelans++;
    		else if (valueCount == 4)
        		carres++;

    		// Détermine la combinaison
    		if (quinteFlush && couleur)
        		return combinaison.QUINTE_FLUSH;
    		else if (carres > 0)
        		return combinaison.CARRE;
    		else if (brelans > 0 && paires > 0)
        		return combinaison.FULL;
    		else if (couleur)
        		return combinaison.COULEUR;
    		else if (quinteFlush)
        		return combinaison.QUINTE;
    		else if (brelans > 0)
        		return combinaison.BRELAN;
    		else if (paires == 2)
        		return combinaison.DOUBLE_PAIRE;
   		 	else if (paires == 1)
        		return combinaison.PAIRE;

    		return combinaison.RIEN;
        }

        // Echange des cartes
        // Paramètres : le tableau de 5 cartes et le tableau des numéros des cartes à échanger
        private static void echangeCarte(carte[] unJeu, int[] e)
        {
		  Random rnd = new Random();

    		for (int i = 0; i < e.Length; i++)
   			{
        		// Vérifie si le numéro de carte à échanger est valide
        		if (e[i] >= 0 && e[i] < unJeu.Length)
        		{
            		// Génère une nouvelle carte
            		unJeu[e[i]].valeur = valeurs[rnd.Next(valeurs.Length)];
            		unJeu[e[i]].famille = familles[rnd.Next(familles.Length)];

            		// Affiche la carte à échanger et la nouvelle carte
            		Console.WriteLine("Vous échangez la carte {unJeu[e[i]].valeur} de {SymboleFamille(unJeu[e[i]].famille)} avec {unJeu[e[i]].valeur} de {SymboleFamille(unJeu[e[i]].famille)}");
        		}
    		}
		
        }

        // Pour afficher le Menu pricipale
        private static void afficheMenu()
        {

                    SetConsoleTextAttribute(hConsole, 012);
    Console.WriteLine("");
    Console.WriteLine("*----------*");
    Console.WriteLine("|   POKER  |");
    Console.WriteLine("|1. Jouer  |");
    Console.WriteLine("|2. Scores |");
    Console.WriteLine("|3. Quitter|");
    Console.WriteLine("*----------*");
    Console.WriteLine("");
                    SetConsoleTextAttribute(hConsole, 014);
    Console.Write("Choisissez une option (1, 2 ou 3) : ");

        	
        }

        // Jouer au Poker
		// Ici que vous appellez toutes les fonction permettant de joueur au poker
        private static void jouerAuPoker()
        {
			Console.Clear(); // Efface le contenu de la console avant de commencer le jeu
        	
        	// Tirage initial du jeu
    		tirageDuJeu(MonJeu);

    		// Affichage du jeu initial
    		affichageCarte();

    		// Demande d'échange de cartes
    		// À vous de compléter cette partie en appelant la fonction d'échange des cartes
    		// e.g., echangeCarte(MonJeu, echange);

   	 		// Affichage du jeu après l'échange
   			 affichageCarte();

    		// Calcul et affichage du résultat
    		afficheResultat(MonJeu);

    		// Enregistrement du score
    		enregistrerJeu(MonJeu);
    
        }

        // Tirage d'un jeu de 5 cartes
        // Paramètre : le tableau de 5 cartes à remplir
        private static void tirageDuJeu(carte[] unJeu)
        {
		 	Random rnd = new Random();

   		 	for (int i = 0; i < unJeu.Length; i++)
    		{
        		carte nouvelleCarte;

        		// Génère une nouvelle carte tant qu'elle n'est pas unique dans le jeu
        		do
        		{
            		nouvelleCarte = tirage();
        		}
        		while (!carteUnique(nouvelleCarte, unJeu, i));

        		unJeu[i] = nouvelleCarte;
    		}
        	
       }

        // Affiche à l'écran une carte {valeur;famille} 
        private static void affichageCarte()
        {
            //----------------------------
            // TIRAGE D'UN JEU DE 5 CARTES
            //----------------------------
            int left = 0;
            int c = 1;
            // Tirage aléatoire de 5 cartes
            for (int i = 0; i < 5; i++)
            {
                // Tirage de la carte n°i (le jeu doit être sans doublons !)

                // Affichage de la carte
                if (MonJeu[i].famille == 3 || MonJeu[i].famille == 4)
                    SetConsoleTextAttribute(hConsole, 252);
                else
                    SetConsoleTextAttribute(hConsole, 240);
                Console.SetCursorPosition(left, 5);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '*', '-', '-', '-', '-', '-', '-', '-', '-', '-', '*');
                Console.SetCursorPosition(left, 6);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, '|');
                Console.SetCursorPosition(left, 7);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|');
                Console.SetCursorPosition(left, 8);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', (char)MonJeu[i].famille, ' ', ' ', ' ', ' ', ' ', ' ', ' ', (char)MonJeu[i].famille, '|');
                Console.SetCursorPosition(left, 9);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', ' ', ' ', ' ', (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, ' ', ' ', ' ', '|');
                Console.SetCursorPosition(left, 10);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', (char)MonJeu[i].famille, ' ', ' ', (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, ' ', ' ', (char)MonJeu[i].famille, '|');
                Console.SetCursorPosition(left, 11);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', ' ', ' ', ' ', (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, (char)MonJeu[i].valeur, ' ', ' ', ' ', '|');
                Console.SetCursorPosition(left, 12);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', (char)MonJeu[i].famille, ' ', ' ', ' ', ' ', ' ', ' ', ' ', (char)MonJeu[i].famille, '|');
                Console.SetCursorPosition(left, 13);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|');
                Console.SetCursorPosition(left, 14);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '|', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, ' ', (char)MonJeu[i].famille, '|');
                Console.SetCursorPosition(left, 15);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", '*', '-', '-', '-', '-', '-', '-', '-', '-', '-', '*');
                Console.SetCursorPosition(left, 16);
                SetConsoleTextAttribute(hConsole, 10);
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}\n", ' ', ' ', ' ', ' ', ' ', c, ' ', ' ', ' ', ' ', ' ');
                left = left + 15;
                c++;
            }

        }

        // Enregistre le score dans le txt
        private static void enregistrerJeu(carte[] unJeu)
        {
    			Console.Write("Entrez votre nom ou pseudo : ");
    			string nom = Console.ReadLine();

    			// Création ou ouverture du fichier scores.txt en mode écriture (FileMode.Append)
    			using (StreamWriter writer = new StreamWriter("scores.txt", true))
    			{
        			// Format de l'enregistrement : nom;famille1valeur1;famille2valeur2;...;famille5valeur5
       				 writer.Write("{nom};");
        			foreach (carte carte in unJeu)
        			{
            			writer.Write("{carte.famille}{carte.valeur};");
        			}
        			writer.WriteLine(); // Passer à une nouvelle ligne pour le prochain enregistrement
    			}

   	 			Console.WriteLine("Le jeu a été enregistré avec succès !");
			}

        // Affiche le Scores
        private static void voirScores()
        {
          	try
    		{
        		// Lecture du fichier scores.txt
        		using (StreamReader reader = new StreamReader("scores.txt"))
        		{
        			Console.WriteLine("");
            		Console.WriteLine("Scores enregistrés :");

            		// Lire et afficher chaque ligne du fichier
            		while (!reader.EndOfStream)
            		{
                		string ligne = reader.ReadLine();
                		Console.WriteLine(ligne);
            		}
        		}
    		}
    		catch (FileNotFoundException)
    		{
        		Console.WriteLine("Aucun score n'a été enregistré pour le moment.");
    		}
    		catch (Exception ex)
    		{
        		Console.WriteLine("Une erreur s'est produite : {ex.Message}");
    		}
        	
        }

        // Affiche résultat
        private static void afficheResultat(carte[] unJeu)
        {
            SetConsoleTextAttribute(hConsole, 012);
            Console.Write("RESULTAT - Vous avez : ");
            try
            {
                // Test de la combinaison
                switch (chercheCombinaison(ref MonJeu))
                {
                    case combinaison.RIEN:
                        Console.WriteLine("rien"); break;
                    case combinaison.PAIRE:
                        Console.WriteLine("une paire"); break;
                    case combinaison.DOUBLE_PAIRE:
                        Console.WriteLine("une double paire"); break;
                    case combinaison.BRELAN:
                        Console.WriteLine("un brelan"); break;
                    case combinaison.QUINTE:
                        Console.WriteLine("une quinte"); break;
                    case combinaison.FULL:
                        Console.WriteLine("un full"); break;
                    case combinaison.COULEUR:
                        Console.WriteLine("une couleur"); break;
                    case combinaison.CARRE:
                        Console.WriteLine("un carre"); break;
                    case combinaison.QUINTE_FLUSH:
                        Console.WriteLine("une quinte-flush"); break;
                };
            }
            catch { }
        }


        //--------------------
        // Fonction PRINCIPALE
        //--------------------
        static void Main(string[] args)
        {
            //---------------
            // BOUCLE DU JEU
            //---------------
            char reponse;
            while (true)
            {
                afficheMenu();
                reponse = (char)_getche();
                if (reponse != '1' && reponse != '2' && reponse != '3')
                {
                    Console.Clear();
                    afficheMenu();
                }
                else
                {
                SetConsoleTextAttribute(hConsole, 015);
                // Jouer au Poker
                if (reponse == '1')
                {
                    int i = 0;
                    jouerAuPoker();
                }

                if (reponse == '2')
                    voirScores();

                if (reponse == '3')
                    break;
            }
            }
            Console.Clear();
        }
    }
}



