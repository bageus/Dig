#Sequenz 2170 - Ende Tutorial, Odin instruiert den Spieler und die Elfe
sq_text file Tournament
adaptive_sound markerdisable
+adaptive_sound primary odin

+adaptive_sound changetheme odin
sq_audio open tourn_2170
sq_wait all

+option set showUI 1

sq_pen set odin [Getobjpos Odin]
sq_pen move odin {0 -1.3 0}
sq_pen set totale odin
sq_pen move totale {-0.7 2 1.7}
sq_actor find Odin 30 1 any odin
sq_pen set weg_mit_dem_odin [Getobjpos Odin]
sq_pen move weg_mit_dem_odin {-40 -6 -5}
sq_pen set zwischendenbeiden odin
sq_pen move zwischendenbeiden {2 0 3}
sq_pen set elfesitzen odin
sq_pen move elfesitzen {7 2 3}
sq_pen set bloedeelfe elfesitzen
sq_pen move bloedeelfe {0 -1 0}
sq_pen set elfekam elfesitzen
sq_pen move elfekam {0 -0.8 0}
sq_pen set ooooodin elfekam
sq_pen move ooooodin {0 -6 0}
sq_pen set elfehoch elfekam
sq_pen move elfehoch {0 -20 0}
sq_pen set elfenstart elfesitzen
sq_pen move elfenstart {8 16 0}
sq_color 0 Odin

sq_wait none
sq_camera fix totale 1.9 0.2 -0.6
do_elf path elfenstart elfesitzen
start_fade 5 1

#do_action anim odin.d_2170_warten 0
#do_wait time 1
do_action anim odin.d_2170_warten 0
do_wait time 2
do_action anim odin.d_2170_warten 0
do_wait time 1
sq_camera move odin 1.1 0.2 -0.6 0.2
do_action anim odin.d_2170_duals 0 ;#Odin beginnt seine Endlosanimation
do_text 2170a 0 {NoAnim} Duals;#"Du, als Wiggel der Wiggels"
do_wait time 5
do_text 2170b 0 {NoAnim} Eswird;#"Es wird ein beschwerlicher"
do_wait time 3
do_text 2170c 0 {NoAnim} Ein;#"... Ein Weg voller Fallen"
do_elf path elfesitzen bloedeelfe
do_wait time 9
#do_action anim odin.d_2170_einweg 0
do_text 2170d 0 {NoAnim} Einweg {40 10}
do_wait time 2
#sq_wait elf
sq_camera move elfekam 0.9 -0.2 0.1
elf unfollowview
do_elf anim kopf_schuetteln
do_wait time 0.5
do_elf anim kopf_schuetteln
do_wait time 0.5
do_action anim odin.d_2170_duals 0 ;#nur damit er sich überhaupt bewegt
do_wait time 1
do_elf anim meckern
do_wait time 2.5
do_elf anim verzweifeln
do_wait time 2
do_text 2170d2 0 {NoAnim} Einweg2 {40 10}
#sq_camera fix totale 1.9 0.1 -0.7
do_wait time 1
do_elf idle
do_wait time 1.5
#sq_camera fix bloedeelfe 1.1 -0.2 0.1
do_wait time 1.5

start_fade 3 0
do_wait time 2
#sq_color 0 Offtext
#do_text 2170k 0 {NoAnim} dummy {1,1} Off
elf unfollowview
do_wait time 0.5
#do_text 2170k 0 {NoAnim} dummy {1,1} Off
do_wait time 0.5
#do_text 2170k 0 {NoAnim} dummy {1,1} Off
do_wait time 0.5
do_elf sleep
#do_text 2170k 0 {NoAnim} dummy {1,1} Off
do_wait time 0.5
#do_text 2170k 0 {NoAnim} dummy {1,1} Off
do_wait time 0.5
#do_text 2170k 0 {NoAnim} dummy {1,1} Off
do_wait time 0.5
#sq_color 0 Odin
do_action anim odin.d_2170_warten 0

start_fade 3 1

do_wait time 2.5
do_text 2170e 0 {NoAnim} Vollerhindernisse;#"Voller Hindernisse..."
do_action anim odin.d_2170_warten 0
do_wait time 5
do_action anim odin.d_2170_warten 0
do_elf standard
do_wait time 0.5
do_elf lookat 0
do_wait time 0.5
sq_camera fix zwischendenbeiden 1.6 0.1 0.7
do_action anim odin.d_2170_warten 0
do_elf text 2170f {kopf_schuetteln} Undvoller;#"und großen Ansprachen"
do_wait time 2
do_action anim odin.d_2170_warten 0
do_wait time 2
do_action anim odin.d_2170_warten 0
do_wait time 1
do_action anim odin.d_2170_deswegen 0;#Umdrehen zur Elfe
do_wait time 1
do_text 2170g 0 {NoAnim} Genaudeswegen;#"Genau! Deswegen..."
do_wait time 3
sq_camera fix elfekam 0.9 0 0.9
do_wait time 0.5

sq_wait none
do_elf lookat
do_wait time 0.5

do_action beam weg_mit_dem_odin 0

do_elf text 2170h {pirouette|gucken_rechts} Sicherdas;#"Sicher, das könnte..."
do_wait time 6
sq_camera fix elfekam 0.9 0 -0.8
do_wait time 2
#do_elf anim schmollen
sq_camera fix elfekam 1.1 0 -0.8
do_elf text 2170i {gucken_links} Odinodin;#"Odin?"
do_wait time 2
sq_camera fix elfekam 1.8 0 -0.8;#ooooodin 1.8 0 -0.8
do_elf path bloedeelfe elfehoch
do_wait time 3
start_fade 1 0
+option set showUI 0
+do_elf hide
+adaptive_sound markerenable
