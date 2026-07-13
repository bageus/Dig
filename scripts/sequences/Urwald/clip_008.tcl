#Urw_unq_feen_Start - Sequenz 8 - Auftrag der Feen
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow feen
#-----------------------------------------
sq_text file Urwald
sq_audio open Clip_8

	+sq_pen set First [Getobjpos Info_Pos_Zwerg]

	+sq_pen set FirstZwerg First
	+sq_pen move FirstZwerg {3 0 1}
	+sq_pen form FirstZwerg Circle 5

	+sq_pen set Mitte [Getobjpos Info_Pos_Troll]

	+sq_pen set MitteZwerg Mitte
	+sq_pen move MitteZwerg {0 0.8 0}

	+sq_pen set MitteZwergC MitteZwerg
	+sq_pen move MitteZwergC {-0.1 0.4 0}

	+sq_pen set MitteZwergD MitteZwerg
	+sq_pen move MitteZwergD {0 0.7 0}

	+sq_pen set Mittecam Mitte
	+sq_pen move Mittecam {1 -1.2 0}

	+sq_pen set Mittecam2 Mitte
	+sq_pen move Mittecam2 {2 -1 4}

	+sq_pen set Mittefee Mitte
	+sq_pen move Mittefee {0.7 -1.1 2.3}

	+sq_pen set MitteElf Mitte
	+sq_pen move MitteElf {-4 -1 16}

	+sq_pen set Elf2 Mitte
	+sq_pen move Elf2 {-1.5 -1.5 16}
	+sq_pen set Elf22 Elf2
	+sq_pen move Elf22 {1 0 -4}


	+sq_pen set Elfelf Elf22
	+sq_pen move Elfelf {0.4 1.4 -1}

   	+sq_pen set FirstC First
   	+sq_pen move FirstC {3 -0.3 0}

	+sq_pen set Mitteend Mitte
	+sq_pen move Mitteend {-0.6 0 0}

	+sq_pen set Rechts [Getobjpos Info_Pos_ZwergTmp]

	+sq_pen set Elfstart MitteElf
	+sq_pen move Elfstart {-15 -15 0}

    +sq_pen set Rechtself Rechts
    +sq_pen move Rechtself {-1 1.5 0}

    +sq_pen set Rechtscam1 Rechts
	+sq_pen move Rechtscam1 {-0.4 0.8 0}

+sq_pen set Feen Mitte
+sq_pen move Feen {0.6 0 1}

sq_wait none

#+do_fee back
sq_camera fix 0 0.9 -0.2 0
do_wait camera

sq_camera follow 0 0.9
do_action walk FirstZwerg 0
do_elf beam Elfstart
do_wait time 9.5
do_action anim scout 0
do_wait time 0.5

sq_camera move Mittecam 2 -0.1 0.3
do_wait camera
do_action anim scratchhead 0
do_elf path Elfstart MitteElf
do_wait time 1.5
sq_actor actionlist {0...} {{{anim standloopa} {anim standloopb}} loop}
do_action anim standloopa 0
sq_actor find Info_Pos_ZwergTmp 30 1
do_wait time 3.5

do_elf lookat Rechtself
do_wait time 0.5
do_elf text 008a {zeigen_rechts} HiFeen
#"Hi, schön euch zu sehen..."
do_wait time 2
sq_camera fix Rechtscam1 1.1 {0 0.5 0}

#do_elf path MitteElf Elfelf 0.3
do_elf move Elfelf
do_fee move Feen
do_fee dither 3
#do_wait time 1
do_elf lookat Rechtself
do_wait fee

sq_wait none

sq_camera move Mittecam2 1.7 {0 0.2 0}
do_wait time 1
-do_fee text 008c Sei
#"Sei gegrüsst ... |Seit wann..."

+sq_pen set Rechtscam2 Rechts
+sq_pen move Rechtscam2 {0 0.7 2}
+sq_pen set Elfcam Elf2
+sq_pen move Elfcam {1 1 30}
do_wait time 5

sq_camera fix Elfcam 1.2 0 -0.5 SelfRot
sq_actor actionlist 0 {{{anim footbaga} {anim footbagb}} loop}
do_action anim footbaga 0

do_fee move Feen

do_wait time 0.4
do_elf lookat 1
do_elf text 008d {ablehnen} Ueberhaupt
#"Ueberhaupt nicht..."
do_wait time 2
do_action anim breathe 0
do_wait time 1.8

#elf unfollowview
do_elf lookat 1
-do_fee text 008e HoerIch
#"Hoer ich solches..."
do_wait time 3.5

sq_camera move Elfcam 1.1 0 0.1
do_elf anim pirouette
do_wait time 2
do_elf lookat 1
do_wait time 0.4
do_elf text 008f {reden_b|reden_a} DuSagst
#"Du sagt es...
do_wait time 2.4;#2

do_fee move Rechts
-do_fee text 008g Schrecklich
#"Schrecklich ..." #"...kriegt kein Wasser mehr..."
sq_camera fix Rechtscam1 1.3
do_wait time 1
do_elf lookat
do_wait time 6;#10

do_fee dither 3
sq_camera fix Rechtscam1 1.0;       #?! vorher 0.9
-do_fee text 008h
do_elf path Elfelf Rechtself 0.3
do_wait time 6.8;#6
do_elf lookat 1

sq_wait none
do_wait time 0.3
do_elf text 008i {kopf_schuetteln} AchDu
#"Ach du Schreck."
do_wait time 1.5;#0.6


sq_camera move Rechtscam1 0.9
do_elf text 008j {verzweifeln} Dann
#"Ihr werdet alle..."
do_action rotate left 0
do_wait time 2.1

sq_actor actionlist 0 {{anim stretch} {anim stretch}}
do_action anim stretch 0

do_fee dither 3
do_elf text 008l {reden_a|gucken_links} Wart

do_wait time 4.3
do_elf lookat
sq_camera fix FirstC 1.1 -0.3 -0.5

sq_actor actionlist 0 {{anim wait} {anim scratch}}
do_action anim scratchhead 0

sq_wait elf
do_elf lookat 0

sq_wait none
do_elf text 008m {auffordern} Hey
#"Hey! Ihr bartlosen..."
do_wait time 2
do_fee dither 3
#sq_camera fix Rechtscam1 1 -0.3 0
sq_camera fix Rechtscam2 1.0 0.3 -0.2 1
do_wait time 2.5

do_elf lookat MitteElf
sq_camera fix FirstC 0.9 -0.1 -0.5

gametime factor 0.3
do_action anim stretch 0
gametime factor 1
do_wait time 0.9
#sq_wait none

-screenvibe 0.8 0.3 0.7 0.7 80 0.1 3
do_elf text 008n {verzweifeln} Wiggles
do_action rotate right 0
do_fee dither 3
do_wait time 1.5

#sq_wait all

#"WIGGELS!"

sq_wait none
sq_camera fix Rechtscam2 1.0 0.3 -0.2 1
do_wait time 1.0

do_elf lookat ElfStart
do_elf text 008o {anfeuern} Los
#"Los, macht..."
do_wait time 2

sq_camera fix Mitte 1.25 -0.1 -0.2
do_action walk MitteZwergD 0
do_fee move Feen
do_fee dither 3
do_wait time 4

do_elf lookat 0
-do_fee text 008p DieTrolle
#"Die Trolle..."
do_wait time 5;#8

-do_fee text 008q
do_wait time 5;#4

sq_wait 0
;#sq_camera fix MitteZwergC 0.7 0 0.3
do_wait camera
#do_wait time 2
+sq_camera move Mitteend 1.4 -0.1
do_wait camera
+do_action beam MitteZwergD 0
+do_elf hide
+do_fee back

+sq_sound Nichts 0
+gametime factor 1

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker feen [get_pos this]
#-----------------------------------------


