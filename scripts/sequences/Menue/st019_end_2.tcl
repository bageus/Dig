//nicht zur freien verfügung!

set_fow_begin 100
sound animsound 0
start_fade 2 0
do_wait time 3
adaptive_sound volfact 80
do_wait time 0.1
adaptive_sound volfact 60
do_wait time 0.1
adaptive_sound volfact 40
do_wait time 0.1
adaptive_sound volfact 20
do_wait time 0.1
adaptive_sound volfact 0

sq_pen set Cam01 TriggerPos
sq_pen set Zwerg01 TriggerPos
sq_pen move Cam01 {-1.7 -0.7 0}
sq_pen setz Cam01 14
sq_pen setz Zwerg01 14
sq_object summon Zwerg Zwerg01 6
sq_wait none
do_wait
sq_camera fix Cam01 0.8 -0.1 -0.95
do_wait
sq_actor find Zwerg 10 1 6 Zwerg01
set_roty [Object 0] 4.71
set_textureanimation [Object 0] 0 16
set_textureanimation [Object 0] 1 15
set_textureanimation [Object 0] 2 7
set_textureanimation [Object 0] 3 12
set_textureanimation [Object 0] 4 4
set_anim [Object 0] mann.schiessen_slomo 31 0
change_particlesource [Object 0] 1 27 {-1.3 -0.35 -0.08} {0 0 0.1} 20 2 0 0 0.5
change_particlesource [Object 0] 2 27 {-0.9 -0.58 0.1} {0 0 0.1} 20 2 0 0 0.5
set_particlesource [Object 0] 1 1
set_particlesource [Object 0] 2 1
do_wait time 1
adaptive_sound changethemenow abspann
adaptive_sound volfact 20
do_wait time 0.1
adaptive_sound volfact 40
do_wait time 0.1
adaptive_sound volfact 60
do_wait time 0.1
adaptive_sound volfact 80
do_wait time 0.1
adaptive_sound volfact 100
do_wait time 0.1
do_wait time 0.5
start_fade 2 1
do_wait time 0.4
particle_processing 0
sound play maxpain 0.7
sq_camera addset neu 1.0 { { s 1.0 1.0 } }
sq_camera selset neu
sq_camera move Cam01 0.8 -0.1 0.95 0.07
do_wait time 4
scroller start data/gui/scroller3.bin
do_wait time 1
start_fade 2 0
do_wait time 3
sq_object delete all
do_wait
do_wait time 8
+particle_processing 1
-scroller start data/gui/scroller5.bin
reset
do_wait time 23
adaptive_sound changetheme abspann2
adaptive_sound primary abspann2
adaptive_sound marker abspann2 [get_pos this]
do_wait time 9
#################################################################################################
sq_wait none
#Erster

sq_pen set Cam1Pos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move Cam1Pos {-10 -0.7 0}
sq_pen set Zwerg1Pos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move Zwerg1Pos {-7 0 -0}
sq_pen set Zwerg2Pos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move Zwerg2Pos {-16 0 -5}
sq_pen form Zwerg2Pos Circle 3
sq_pen set EPos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move EPos {-10 -0.5 1.5}
do_wait
sq_camera fix Cam1Pos 0.9 -0.1 -0.1
sq_object summon Zwerg Zwerg1Pos
call_method [Object 0] Editor_Set_Info {{name Fred_Durst} {gender male}}
call_method [Object 0] init
sq_pen move Zwerg1Pos { -0.5 0 -1 }
sq_object summon Zwerg Zwerg1Pos
call_method [Object 1] Editor_Set_Info {{name Fred_Feuerstein} {gender male}}
call_method [Object 1] init
sq_pen move Zwerg1Pos { 1 0 0 }
sq_object summon Zwerg Zwerg1Pos
call_method [Object 2] Editor_Set_Info {{name Right_Said_Fred} {gender male}}
call_method [Object 2] init

set_textureanimation [Object 0] 3 5
set_textureanimation [Object 1] 3 5
set_textureanimation [Object 2] 3 5
set_textureanimation [Object 0] 4 2
set_textureanimation [Object 1] 4 2
set_textureanimation [Object 2] 4 2

sq_object summon Dummy_Muetze_a
sq_object summon Dummy_Muetze_a
sq_object summon Dummy_Muetze_a
do_wait
link_obj [Object 3] [Object 0] 4
link_obj [Object 4] [Object 1] 4
link_obj [Object 5] [Object 2] 4

elf texvar 5
do_elf beam EPos
sq_actor find Zwerg 10 3
do_wait time 2
sq_actor actionlist {0 1 2} {}
+start_fade 1.5 1
do_elf anim koketieren
do_wait time 2.1
do_elf anim kusshand
do_wait time 2.1
+elf hide
do_wait time 3
do_action flee Zwerg2Pos { 0 }
do_wait time 0.3
do_action flee Zwerg2Pos { 1 }
do_wait time 0.1
do_action flee Zwerg2Pos { 2 }
do_wait time 4
+start_fade 0.5 0
do_wait time 2



+sq_object delete all
reset

do_wait time 25

#################################################################################################

sq_pen set C1 	[Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move C1 {-13 0 -4}
sq_camera fix C1 1.38 -0.355 0.165
sq_pen set P_Start 	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen set P_End 	[Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move P_End {-6 0 0}
sq_pen set P_End2 	[Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move P_End2 {-2 0 0}
sq_pen set P_End4 	[Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move P_End4 {-1.5 0 0}
sq_pen set P_End3 	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen move P_End3 { 10.5 0 -3 }
sq_pen set P_Mitte1	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen move P_Mitte1 { 5 0 -2.5 }
sq_pen set P_Mitte2	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen move P_Mitte2 { 6.5 0 -3 }
sq_pen set P_MitteCam	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen move P_MitteCam { 5.75 0 -3 }
sq_pen set P_MitteCam2	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen move P_MitteCam2 { 11 0 -2 }
sq_pen set P_MitteCam3	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen move P_MitteCam3 { 9 0 -10 }
sq_pen set P_MitteCam4	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen move P_MitteCam4 { 10.5 0 -2 }
sq_pen set P_Mitte3	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen move P_Mitte3 { 10 0 -2.5 }
sq_pen set P_Mitte4	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen move P_Mitte4 { 8.2 0 0 }
sq_pen setz P_Mitte4 0.5
sq_pen set P_Death	[Getobjpos Info_Pos_ZwergTmp 0 10000]
sq_pen move P_Death { 12 -0.75 0.5 }

sq_object summon Zwerg P_Start
call_method [Object 0] Editor_Set_Info {{name Fred} {gender male}}
call_method [Object 0] init
do_wait time 0.2
sq_actor find Zwerg
do_wait time 0.1
sq_object summon Zwerg P_Start
call_method [Object 1] Editor_Set_Info {{name Fredine} {gender female}}
call_method [Object 1] init
do_wait time 0.2
sq_actor find Zwerg
do_wait time 0.1
sq_object summon Dummy_Muetze_a P_Start
sq_object summon Dummy_Muetze_b P_Start
do_wait
link_obj [Object 2] [Actor 0] 4
link_obj [Object 3] [Actor 1] 4
do_wait time 1
sq_object summon Spitzhacke P_End2
sq_actor express 0 bad_normal
sq_wait none
sq_actor actionlist 1 { { anim standloopa } { anim standloopb } { anim standloopc } { rotate 0 } { anim talkrentb } { anim talkrengb } { rotate P_End } { anim talkacngb } { walkfit P_End } }
do_action walkfit P_Mitte2 1
do_wait time 2
start_fade 2 1
do_wait time 3
do_action flee P_Mitte1 0
sq_wait 0
do_wait
do_action rotate 1 0
do_action anim talkacnta 0
do_action anim talkacpoa 0
sq_camera fix P_MitteCam 0.65 -0.1 -0.9
do_action anim talkacpob 0
do_action anim talkacntc 0
do_action anim talkrepob 0
do_action anim talkacpob 0
sq_actor express 0 bad_dizzy
do_wait time 0.5
do_action anim scratchhead 0
do_wait time 0.5
sq_wait none
do_action flee P_Mitte3 0
do_wait time 1.5
sq_camera fix P_MitteCam2 1 -0.3 0.6
sq_wait 0
do_wait
sq_camera selset inout
do_wait time 0.5
do_action rotate 1 0
sq_actor actionlist 1 { { anim talkacnga } { walkfit P_End2 } }
do_action rotate 0 1
do_action anim standloopa 0
do_action anim standloopb 0
do_action anim scratchhead 0
do_action anim standloopa 0
sq_camera move P_MitteCam3 0.65 -0.1 0.2 0.3
do_wait time 0.3
do_action walktired P_Mitte4 0
do_action wait 0.5 0
do_action rotate front 0
do_action wait 1 0
do_action anim wipenose 0
do_action rotate back 0
do_wait time 0.5
do_action anim talkacnga 0
sq_actor mouth 0 { 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 }
sq_actor eyes 0 { 7 0 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 4 }
do_action rotate P_End 0
do_action anim tooltakeout_a 0
link_obj [Object 4] [Actor 0] 0
do_action anim tooltakeout_b 0
do_action run P_End3 0
sq_camera fix P_MitteCam4 0.8 -0.05 0.4
do_action run P_End4 0
do_particle create 8 P_Death {-0.2 -0.15 0} 50 1
do_wait time 0.5
do_particle create 8 P_Death {-0.21 -0.1 0} 30 2
start_fade 1.5 0
do_wait time 0.2
do_wait time 0.7
do_wait time 1.5
+sq_object delete all
reset

do_wait time 23
#################################################################################################
sq_wait none
#Erster
sq_pen set Cam1Pos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move Cam1Pos {-10 0 0}

sq_pen set Z1Pos Cam1Pos
sq_pen move Z1Pos {3.3 0 -6}

sq_pen set BabyPos Cam1Pos
sq_pen move BabyPos {-1 0 -1.5}

sq_pen set Baby2Pos BabyPos
sq_pen move Baby2Pos {1 0 0}


#0
sq_object summon Baby BabyPos
#1
sq_object summon Zwerg Z1Pos 1
call_method [Object 1] init
#2
sq_object summon Dummy_Voodoo_Muetze_a
#3
sq_object summon Dummy_Joint_a
#4
sq_object summon Stein;set_anim [Object 4] joint_c.standard 0 0;link_obj [Object 4] [Object 1] 10
#5
sq_object summon Stein;set_anim [Object 5] joint_c.standard 0 0

link_obj [Object 2] [Object 1] 11

sq_actor find Baby
sq_actor find Zwerg

change_particlesource [Actor 1] 5 6 {0 0 0} {0 0 0.3 } 20 20 0 10 0 0
set_particlesource [Actor 1] 5 1


sq_camera fix Cam1Pos 0.8 -0.1 -0.5
do_wait time 1
+start_fade 1.5 1
do_action walk Baby2Pos 1
do_wait time 4.5
set_particlesource [Actor 1] 5 0
do_action rotate right 0
do_wait time 2.0
set_particlesource [Actor 1] 5 1
do_action anim baby.auf_po_fallen 0
do_wait time 2.5
set_particlesource [Actor 1] 5 0
do_action anim scratchhead 1
do_wait time 1.5
set_particlesource [Actor 1] 5 1
do_wait time 1.5
set_particlesource [Actor 1] 5 0
do_wait time 1.5
link_obj [Object 3] [Object 1] 0
do_action anim offerjoint 1
do_wait time 1
link_obj [Object 3];set_pos [Object 3] [parse_pos Z1Pos];link_obj [Object 5] [Object 0] 10
change_particlesource [Actor 0] 5 6 {0 0 0} {0 0 0.3 } 20 20 0 10 0 0
do_wait time 1
do_action anim baby.essen_start 0
do_wait time 0.7
do_action anim baby.essen_loop 0
do_wait time 0.7
set_particlesource [Actor 0] 5 1
do_action anim baby.essen_loop 0
do_wait time 0.7
do_action anim baby.essen_loop 0
do_wait time 0.7
do_action anim baby.essen_loop 0
do_wait time 1
set_particlesource [Actor 0] 5 0
link_obj [Object 5];set_pos [Object 3] [parse_pos Z1Pos]
do_wait time 1
sq_actor actionlist 0 { {anim baby.schlafen_loop} loop }
do_action anim baby.einschlafen 0
do_wait time 1
do_action rotate 5.8 1
do_wait time 0.5
#sq_actor mouth 1 { 5 }
do_wait time 0.5
set_particlesource [Actor 1] 5 1
do_wait time 1
set_particlesource [Actor 1] 5 0
do_wait time 0.5
do_action walk Z1Pos 1
do_wait time 3.0
+start_fade 0.5 0
do_wait time 1
+del [ref_get [Actor 1] myhairs]
+sq_actor actionlist 0 {}
+sq_object delete all
reset
do_wait time 35
#################################################################################################
#################################################################################################
#################################################################################################
+start_fade 1.5 1
sq_wait none
#Erster
sq_pen set Cam1Pos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move Cam1Pos {-9.5 -0.5 2}

sq_pen set ZwergPos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move ZwergPos {-11.5 0 -4}

sq_pen set Zwerg2Pos ZwergPos
sq_pen move Zwerg2Pos {1.3 0 -1}

sq_pen set Zwerg3Pos ZwergPos
sq_pen move Zwerg3Pos {0.3 0 -1}

sq_pen set Hit1Pos ZwergPos
sq_pen move Hit1Pos {-3.5 0 -1}

sq_pen set HandOffPos ZwergPos
sq_pen move HandOffPos {-3 -1 2}

sq_pen set Hand1Pos ZwergPos
sq_pen move Hand1Pos {1.5 -0.5 2}

sq_pen set Hand2Pos Hand1Pos
sq_pen move Hand2Pos {1.4 -0.5 0}

sq_pen set Hand3Pos Hand1Pos
sq_pen move Hand3Pos {0 -0.2 0}
sq_pen set Keule2Pos Hand3Pos
sq_pen move Keule2Pos {0.1 0.25 -0.2}


sq_pen set Hand4Pos Hand1Pos
sq_pen move Hand4Pos {-0.75 -0.5 0}

sq_pen set Hand5Pos Hand4Pos
sq_pen move Hand5Pos {1.25 0 0}

sq_pen set Hand6Pos Hand5Pos
sq_pen move Hand6Pos {3 0 0}
sq_pen set Keule1Pos Hand6Pos
sq_pen move Keule1Pos {0.1 0.25 -0.2}


#0
sq_object summon Zwerg ZwergPos
#1
sq_object summon Stein HandOffPos
set_physic [Object 1] 0
set_anim [Object 1] mauszeiger.001 0 2
set_rotx [Object 1] 1.57

sq_actor find Zwerg

sq_camera fix Cam1Pos 0.8 -0.1 -0.1
do_wait time 1.5
sq_object move 1 Hand1Pos 0.1
do_wait time 5
set_rotx [Object 1] 1.5
notifyflare 42 68
do_wait time 0.2
set_rotx [Object 1] 1.57
do_wait time 0.2
set_rotx [Object 1] 1.5
notifyflare 42 68
do_wait time 0.2
set_rotx [Object 1] 1.57
do_wait time 0.2
do_wait time 0.5
do_action walk Zwerg2Pos 0
do_wait time 2
sq_object move 1 Hand2Pos 0.1
do_wait time 2
set_anim [Object 1] mauszeiger.006 0 1
set_rotx [Object 1] 1.5
notifyflare 81 50
do_wait time 0.2
set_rotx [Object 1] 1.57
do_wait time 0.2
set_rotx [Object 1] 1.5
notifyflare 81 50
do_wait time 0.2
set_rotx [Object 1] 1.57
do_wait time 0.2
set_anim [Object 1] mauszeiger.001 0 1
do_wait time 1
do_action rotate front 0
do_wait time 1
do_action anim talkacngb 0
do_wait time 4
set_anim [Object 1] mauszeiger.006 0 1
set_rotx [Object 1] 1.5
notifyflare 81 50
do_wait time 0.2
set_rotx [Object 1] 1.57
do_wait time 0.2
set_rotx [Object 1] 1.5
notifyflare 81 50
do_wait time 0.2
set_rotx [Object 1] 1.57
do_wait time 0.2
set_anim [Object 1] mauszeiger.001 0 1
do_wait time 2
do_action anim warmbutt 0
do_wait time 4
sq_object move 1 Hand3Pos 0.3
do_wait time 2
set_rotx [Object 1] 1.4
do_action anim standfronthitl 0
do_wait time 0.2
set_rotx [Object 1] 1.57
do_wait time 0.2
set_rotx [Object 1] 1.4
do_action anim standfronthitl 0
do_wait time 0.2
set_rotx [Object 1] 1.57
do_wait time 0.1
do_action anim scratch 0
do_wait time 0.5
sq_actor eyes 0 {4}
sq_actor mouth 0 {3}

do_wait time 2

sq_object move 1 Hand2Pos 0.1
do_wait time 2
set_anim [Object 1] mauszeiger.006 0 1
set_rotx [Object 1] 1.5
notifyflare 81 50
do_wait time 0.2
set_rotx [Object 1] 1.57
do_wait time 0.2
set_rotx [Object 1] 1.5
notifyflare 81 50
do_wait time 0.2
set_rotx [Object 1] 1.57
do_wait time 0.2
set_anim [Object 1] mauszeiger.001 0 1
do_wait time 1
do_action anim mann.dialog_ac_negativ_c 0
do_wait time 3
sq_object move 1 Hand4Pos 0.5
do_wait time 0.25
do_action anim standbackhitm 0
do_wait time 0.4
sq_object move 1 Hand5Pos 0.5
do_wait time 0.05
do_action anim standfronthitm 0
do_wait time 2
do_action anim mann.dialog_re_negativ_a 0
do_wait time 2
do_tooltakeout Axt 0
do_wait time 0.5
do_action rotate 4.9 0
do_wait time 0.75
do_action anim standtosword 0
do_wait time 0.5
do_action anim swordheadstab 0
do_wait time 0.1
change_particlesource [Object 1] 1 8 {0 0 0} {0 0 0} 255 10 0 5 0 1
set_particlesource [Object 1] 1 1
sq_object move 1 Hand2Pos 0.3
do_wait time 0.5
set_particlesource [Object 1] 1 0
do_wait time 0.5
do_action anim swordturn 0
do_wait time 1
do_action anim talkacngb 0
do_wait time 1
sq_object move 1 Hand6Pos 0.2
+do_toolputaway 0
do_wait time 1
do_action rotate front 0
sq_actor eyes 0 {0}
sq_actor mouth 0 {9}
do_wait time 1
do_action anim mann.dialog_pa_neutral_c 0
do_wait time 1
do_action rotate left 0
do_wait time 0.5
do_action walk Hit1Pos 0
do_wait time 3
+start_fade 1.5 0
do_wait time 2
+sq_object delete all
do_wait time 1
reset
do_wait time 50
adaptive_sound changetheme brains_disco
adaptive_sound primary brains_disco

adaptive_sound delmarker [get_pos this]
adaptive_sound marker brains_disco [get_pos this]
do_wait time 5
#################################################################################################
                        #Sascha Seq. 1
#Erster
sq_pen set Zwerg1Pos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move Zwerg1Pos {-6 0 0}
#Zweiter
sq_pen set Zwerg2Pos Zwerg1Pos
sq_pen move Zwerg2Pos {0.5 0 1}
#Dritter
sq_pen set Zwerg3Pos Zwerg1Pos
sq_pen move Zwerg3Pos {0.5 0 -1}
#Erster Ziel
sq_pen set Zwerg1ZielPos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move Zwerg1ZielPos {-9.5 0 -1}
#Zweiter Ziel
sq_pen set Zwerg2ZielPos Zwerg1ZielPos
sq_pen move Zwerg2ZielPos {0.2 0 1.2}
#Dritter Ziel
sq_pen set Zwerg3ZielPos Zwerg1ZielPos
sq_pen move Zwerg3ZielPos {0.2 0 -1.5}
#Wiggle
sq_pen set WigglePos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move WigglePos {-16.5 0 -1}
#Baby
sq_pen set BabyPos WigglePos
sq_pen move BabyPos {0.3 0 0.7}
#BadGuyCam
sq_pen set Cam1Pos [Getobjpos Info_Pos_Zwerg 0 10000]
sq_pen move Cam1Pos {-8.5 0 -2}

sq_pen set Cam2Pos WigglePos
sq_pen move Cam2Pos {0 -0.7 0}
#################################
#0
sq_object summon Zwerg Zwerg1Pos
#1
sq_object summon Zwerg Zwerg2Pos
#2
sq_object summon Zwerg Zwerg3Pos
#3
sq_object summon Troll WigglePos
set_textureanimation [Object 3]  0 2 0 0
set_textureanimation [Object 3]  1 0 0 0
#4

sq_actor find Troll 20 20 any WigglePos
do_wait time 0.2
sq_actor find Zwerg 20 20 any WigglePos
do_wait time 0.2

set_textureanimation [Actor 1] 0 16
set_textureanimation [Actor 1] 1 15
set_textureanimation [Actor 1] 2 7
set_textureanimation [Actor 1] 3 17
set_textureanimation [Actor 1] 4 4

set_textureanimation [Actor 2] 0 16
set_textureanimation [Actor 2] 1 15
set_textureanimation [Actor 2] 2 7
set_textureanimation [Actor 2] 3 17
set_textureanimation [Actor 2] 4 4

set_textureanimation [Actor 3] 0 16
set_textureanimation [Actor 3] 1 15
set_textureanimation [Actor 3] 2 7
set_textureanimation [Actor 3] 3 17
set_textureanimation [Actor 3] 4 4
sq_actor eyes 0 {3}
sq_wait none
######################
sq_pen move Cam1Pos {-0.5 0 0}
sq_camera fix Cam1Pos 0.9 -0.2 0.4
do_action beam Zwerg1ZielPos 1
do_action beam Zwerg2ZielPos 2
do_action beam Zwerg3ZielPos 3
do_action rotate front 0
do_action rotate 1.57 1
do_action rotate 1.57 2
do_action rotate 1.57 3
sq_camera fix Cam1Pos 0.65 -0.2 1.1
do_wait time 0.5
+start_fade 0.5 1
do_action anim scratchhead 0
do_wait time 2
+start_fade 0.5 0
do_wait time 1.5
sq_camera fix Cam2Pos 1 0 0
+start_fade 0.5 1
do_action anim troll.stehen_plakat 0
do_wait time 4
+start_fade 0.5 0
do_wait time 0.5
sq_camera fix Cam1Pos 0.65 -0.2 1.1
do_wait time 1.5
+start_fade 0.5 1
sq_actor actionlist 1 { {anim mann.schiessen_mp5_loop} loop }
sq_actor actionlist 3 { {anim mann.schiessen_mp5_loop} loop }
sq_actor actionlist 2 { {anim mann.schiessen_m3_super_90_loop} loop }
######
#Geknarze 1
change_particlesource [Actor 1] 1 7 {0 -0.75 0.2} {-0.2 0 0} 10 10 0 3 0.7 0
set_particlesource [Actor 1] 1 1
#Geflamme 1
change_particlesource [Actor 1] 2 0 {0 -0.75 0.7} {-0.4 0 0} 10 1 0 3 0.7 0
set_particlesource [Actor 1] 2 1
#Geknarze 3
change_particlesource [Actor 3] 1 7 {0 -0.75 0.2} {-0.4 0 0} 10 10 0 3 0.7 0
set_particlesource [Actor 3] 1 1
#Geflamme 3
change_particlesource [Actor 3] 2 0 {0 -0.75 0.7} {-0.4 0 0} 10 1 0 3 0.7 0
set_particlesource [Actor 3] 2 1
#Geknarze 2
change_particlesource [Actor 2] 1 7 {0 -0.75 0.2} {-0.4 0 0} 10 10 0 3 0.7 0
#Geflamme 2
change_particlesource [Actor 2] 2 0 {0 -0.75 0.7} {-0.4 0 0} 100 20 0 3 0.7 0
######
do_action anim mann.schiessen_mp5_loop 1
do_wait time 0.2
do_action anim mann.schiessen_mp5_loop 3
do_wait time 0.2
do_action anim mann.schiessen_m3_super_90_loop 2
#Erster Schuss
set_particlesource [Actor 2] 1 1
set_particlesource [Actor 2] 2 1
do_wait time 0.05
-scroller start data/gui/scroller6.bin
set_particlesource [Actor 2] 2 0
set_particlesource [Actor 2] 1 0
do_wait time 1.85
#Zweiter Schuss
set_particlesource [Actor 2] 1 1
set_particlesource [Actor 2] 2 1
do_wait time 0.05
set_particlesource [Actor 2] 2 0
set_particlesource [Actor 2] 1 0
do_wait time 1.85
#Dritter Schuss
set_particlesource [Actor 2] 1 1
set_particlesource [Actor 2] 2 1
do_wait time 0.05
set_particlesource [Actor 2] 2 0
set_particlesource [Actor 2] 1 0
do_wait time 0.2
+start_fade 0.5 0
do_wait time 1.5
sq_pen set BloodPos 0
sq_pen move WigglePos {0.5 0 -2}
sq_pen set TTPos WigglePos
sq_pen move TTPos {0.5 0 4}

sq_pen set TTotPos WigglePos
sq_pen move TTotPos {0 0 1}


sq_pen move BabyPos {-0.5 0 0}
set_pos [Actor 0] [parse_pos TTotPos]
set_anim [Actor 0] troll.verwesen_b 0 0
#4
sq_object summon Blutfleck TTPos
#5
sq_pen set Blutwand WigglePos
sq_pen move Blutwand {-3.0 0 0}
sq_object summon Blutfleck Blutwand
set_roty [Object 3] 3.0
set_rotx [Object 5] 1.57
set_posz [Object 5] 11.0
sq_camera fix BloodPos 0.8 0 0
+start_fade 1.5 1
do_wait time 2.5
+start_fade 1.5 0
+sq_object delete all
reset
do_wait time 105
adaptive_sound stop
scroller start data/gui/scroller7.bin
do_wait time 8
sq_camera get
#################################################################################################



+cancel_fade

+sound animsound 1

