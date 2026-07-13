sq_text file Schwefel
sq_audio open 0116
do_wait camera
+sq_actor find Zwerg 10 1
+sq_actor find Drache
+adaptive_sound changethemenow Swf_116
sq_color 1 Drache
sq_wait all
##############TrollPos#########################
+sq_pen set T1 [Getobjpos Info_Drache_Waypoint2]
+sq_pen move T1 {-25 0 2}

+sq_pen set T2 T1
+sq_pen move T2 {-3 0 -1}

+sq_pen set T3 T1
+sq_pen move T3 {-3 0 1}

+sq_pen set T4 T1
+sq_pen move T4 {-3 0 -4}

+sq_pen set T5 T1
+sq_pen move T5 {-3 0 2}

+sq_pen set T2Stand T1
+sq_pen move TStand {-0.8 0 -1}

+sq_pen set T3Stand T1
+sq_pen move T3Stand {-0.4 0 1}

+sq_pen set T4Stand T1
+sq_pen move T4Stand {0 0 1.2}

+sq_pen set T4Stand T1
+sq_pen move T4Stand {1.5 0 3.5}

+sq_pen set T5Stand T1
+sq_pen move T5Stand {0.6 0 -2}
########TRun's##############
+sq_pen set TRun1 T1
+sq_pen move TRun1 {12 0 0}

+sq_pen set TRun2 TRun1
+sq_pen move TRun2 {1 0 -4}

+sq_pen set TRun3 TRun1
+sq_pen move TRun3 {0 0 2}

+sq_pen set TRun4 TRun1
+sq_pen move TRun4 {-1 0 -6}

+sq_pen set TRun5 TRun1
+sq_pen move TRun5 {-1.5 0 0}
########TAttack's###############
+sq_pen set TAttack1 TRun1
+sq_pen move TAttack1 {6 0 0}

+sq_pen set TAttack2 TRun2
+sq_pen move TAttack2 {6 0 0}

+sq_pen set TAttack3 TRun3
+sq_pen move TAttack3 {6 0 0}

+sq_pen set TAttack4 TRun4
+sq_pen move TAttack4 {6 0 0}

+sq_pen set TAttack5 TRun5
+sq_pen move TAttack5 {6 0 0}
################################
+sq_pen set WPos 0
+sq_pen move WPos {20 0 0}

############################################
+sq_pen set StopPos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move StopPos {-22 0 2}

+sq_pen set FleePos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move FleePos {2 0 4}

+sq_pen set DrachenPos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move DrachenPos {0 0 0}

+sq_pen set NewDrachenPos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move NewDrachenPos {5 0 1}

+sq_pen set Cam1Pos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move Cam1Pos {-23 0 2}

+sq_pen set Cam2Pos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move Cam2Pos {-20 0 2}

+sq_pen set Cam3Pos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move Cam3Pos {4 -2 -2}

############################################
#0/2
+sq_object summon Troll T1
#1/3
+sq_object summon Troll T2
#2/4
+sq_object summon Troll T3
#3/5
+sq_object summon Troll T4
#4/6
+sq_object summon Troll T5
#5/7
+sq_object summon Keule WPos
#6/8#Wiggle1
+sq_object summon Schwert WPos
#7/9
+sq_object summon Streitkolben WPos
#8/10
+sq_object summon Keule WPos
#9/11
+sq_object summon Hellebarde WPos
#10/12#Wiggle2
+sq_object summon Schwert WPos
#11/13
+sq_object summon Krumsaebel WPos
#12/14
+sq_object summon Hellebarde WPos

############################################
+sq_actor find Troll 20 5
sq_actor eyes 0 {12}
sq_actor mouth 0 {0}
############################################
link_obj [Object 5] [Object 0] 0
link_obj [Object 7] [Object 1] 0
link_obj [Object 8] [Object 2] 0
link_obj [Object 9] [Object 3] 0
link_obj [Object 11] [Object 4] 0

#######################
#Test#
#sq_actor find Zwerg 30 20 0
#######################


sq_wait none
#sq_camera follow 0 1.0
#do_wait time 3
sq_camera move Cam1Pos 1.0 -0.2 0.2
do_wait time 3
sq_wait all
+do_action rotate right 2
+do_action run StopPos 0
+do_action rotate left 0
sq_wait none
+sq_pen set ShowPos 0
+sq_pen move ShowPos {0.08 0.25 0}
+sq_pen set TShowPos 2
+sq_pen move TShowPos {0.08 -0.2 0}
do_action anim shock 0
do_action anim troll.spies_anschreien_b 2
sq_wait all
do_wait time 1
+set_roty [Actor 0] 0
+set_roty [Actor 2] 0
#set_texturevariation [Actor 2] {3}

sq_camera fix TShowPos 0.75 0.2 0.0
do_action anim troll.stehen_fuchteln 2
do_action anim troll.stehen_drohen 2

##############Zwerg#####################
sq_camera fix ShowPos 0.65 -0.1 0.0
sq_actor express 0 tired
do_wait time 1
do_change muetze stone 0 ab
do_action anim warmupcstart 0
do_action anim warmupcloop 0
do_action anim warmupcloop 0
do_action anim warmupstop 0
#do_action anim warmupe 0
sq_actor eyes 0 {4}
link_obj [Object 6] [Actor 0] 1
link_obj [Object 10] [Actor 0] 0

#sound play testsound 1


do_action anim tooltakeoutleft_a 0
do_action anim mann.schwert_b 0
do_action anim mann.schwert_c 0
do_action anim mann.schwert_taenzel 0
do_action anim mann.schwert_a 0


sq_actor eyes 0 {4 13 13 13 4}
do_text "Grrrr...." 0 Auto Grr
do_wait time 1
set_roty [Actor 0] 1.57
set_roty [Actor 2] 4.71

sq_camera fix Cam1Pos 1.0 -0.2 0.1
do_action anim discoc 0
sq_wait none
sq_actor actionlist 0 {{{anim discoc} {anim swordmasterstroke} {anim swordturn} {anim kungfuskip} {anim swordtwist} {anim standloopc} } loop}
+do_action run T2Stand 3
do_action anim swordmasterstroke 0
+do_action run T3Stand 4
+do_action run T4Stand 5
+do_action run T5Stand 6
do_wait time 1


do_wait time 1
sq_camera fix Cam1Pos 0.8 -0.2 -0.7
sq_actor actionlist 0 {}
do_wait time 1
#set_roty [Actor 2] 4.71
+set_roty [Actor 3] 4.71
+set_roty [Actor 4] 4.71
+set_roty [Actor 5] 4.71
+set_roty [Actor 6] 4.31
do_action anim troll.stehen_drohen 2
do_action anim troll.spies_anschreien_b 3
do_action anim troll.stehen_fuchteln 4
do_action anim troll.stehen_drohen 5
do_action anim troll.stehen_fuchteln 6
do_wait time 2


+set_roty [Actor 0] 0
+sq_pen set BigPos 0
+sq_pen move BigPos {0.1 0 4}
+do_action beam BigPos 0
+sq_pen move ShowPos {0 -0.3 -2}
#set_visibility [Object 10] 0
set_visibility [Object 6] 0
link_obj [Object 6]
sq_camera fix ShowPos 0.73 0.0 0.0 2
do_wait time 0.5
sq_actor eyes 0 {3}
sq_camera move ShowPos 0.71 0.0 0.0 2
do_wait time 0.5
sq_actor mouth 0 {2}
sq_camera fix ShowPos 0.69 0.0 0.0 2
do_wait time 0.5
sq_actor eyes 0 {2}
sq_camera fix ShowPos 0.67 0.0 0.0 2
do_wait time 2
set_visibility [Object 6] 1
set_visibility [Object 10] 1


+sq_pen move ShowPos {0 0.3 2}
+sq_pen set ZBigPos 0
+sq_pen move ZBigPos {-0.1 0 -4}
+do_action beam ZBigPos 0
+set_roty [Actor 0] 1.57

link_obj [Object 10]
link_obj [Object 6]

sq_camera fix Cam1Pos 0.8 -0.2 -0.7

+sq_pen set S1 0
+sq_pen move S1 {-0.7 0.1 0}
+sq_pen set S2 0
+sq_pen move S2 {-0.5 0.1 0}
+sq_object beam [Object 6] S1
+sq_object beam [Object 10] S2
+set_rotz [Object 6] 1.5
+set_rotz [Object 10] 1.5

sq_camera fix Cam1Pos 0.8 -0.2 -0.7
+do_action panicflee FleePos 0
do_wait time 2
+do_action run TRun1 2
do_wait time 0.1
+do_action run TRun2 3
do_wait time 0.1
+do_action run TRun3 4
+do_action run TRun4 5
+do_action run TRun5 6
do_wait time 1
sq_pen move Cam1Pos {7 0 0}
sq_camera fix Cam1Pos 0.9 0.0 0.0
do_wait time 3.2
+set_roty [Actor 1] 1.57

#######################################################
#######################################################
+sq_pen move Cam3Pos {-5 0 0}
sq_camera fix Cam3Pos 1.4 0.1 0.7
do_wait time 2
do_wait time 6
+do_action rotate T1 0
do_text 116x 1 {drache.sitzen_zu_schulter_l_d_ihredlen drache.schulter_l_d_ha } Ich_bitte
do_wait time 4
do_text 116y 0 Auto Na_gut
do_wait time 4
sq_wait none
sq_camera fix 2 1.0 -0.2 -0.2
do_action anim troll.stehen_drohen 2
do_action anim troll.spies_anschreien_b 3
do_action anim troll.stehen_fuchteln 4
do_action anim troll.stehen_drohen 5
do_action anim troll.stehen_fuchteln 6
do_wait time 1
sq_camera move 2 1.0 -0.05 0.9 0.2
do_wait time 1
do_action anim troll.stehen_drohen 3
do_action anim troll.spies_anschreien_b 2
do_action anim troll.stehen_fuchteln 6
do_action anim troll.stehen_drohen 4
do_action anim troll.stehen_fuchteln 5
do_wait time 2
+do_action run TAttack1 2
+do_action run TAttack2 3
+do_action run TAttack3 4
+do_action run TAttack4 5
+do_action run TAttack5 6
call_method [Actor 1] set_enemy [Actor 4]
sq_actor idleanim 1 drache.speien_a_loop
do_action anim drache.speien_a_loop 1
do_wait time 1
call_method [Actor 1] fire2_start
+sq_pen set FirePos 1
+sq_pen move FirePos {-3 -1 0}
sq_camera move FirePos 1.3 -0.3 0.9 0.7
start_fade 4 0
do_wait time 1
do_action anim troll.stehen_explosion_tot 3
do_action anim troll.stehen_exp_verwesen 3


do_wait time 4
+call_method [Actor 1] fire_stop
#sq_wait none
+sq_pen set DeadPos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move DeadPos {-12 0 0}
+sq_pen set BackBeamPos 1
+do_action beam DeadPos 1

+sq_pen set Cam4Pos 1
+sq_pen move Cam4Pos {-2 0 0}
#sq_camera fix Cam4Pos 1.4 -0.4 0.4
sq_actor eyes 0 {8}
sq_actor mouth 0 {7}
do_wait time 3
#sq_camera fix Cam5Pos 0.7 -0.2 0.1
sq_wait all
do_wait camera
+sq_object delete all
sq_pen set BlutPos 1
sq_object summon Blutfleck BlutPos
set_roty [Object 0] 1.4

###################################
+sq_pen set TTot1 [Getobjpos Info_Drache_Waypoint2]
+sq_pen move TTot1 {-27 0 -2}
+sq_pen set TTot2 [Getobjpos Info_Drache_Waypoint2]
+sq_pen move TTot2 {-20 0 -7}
+sq_pen set TTot3 [Getobjpos Info_Drache_Waypoint2]
+sq_pen move TTot3 {-24 0 2}
+sq_pen set TTot4 [Getobjpos Info_Drache_Waypoint2]
+sq_pen move TTot4 {-17 0 -6}
+sq_pen set TTot5 [Getobjpos Info_Drache_Waypoint2]
+sq_pen move TTot5 {-25 0 4}
+sq_pen set TTot6 [Getobjpos Info_Drache_Waypoint2]
+sq_pen move TTot6 {-22 0 -5}


#Actor 2
+sq_object summon Troll TTot1
+sq_object summon Troll TTot2
+sq_object summon Troll TTot3
+sq_object summon Troll TTot4
+sq_object summon Troll TTot5
+sq_object summon Troll TTot6

sq_actor find Troll 20 6

change_particlesource [Actor 7] 1 6 {0 0 0} {0 0 0} 56 5 0 5 0 1
change_particlesource [Actor 8] 1 6 {0 0 0} {0 0 0} 56 5 0 5 0 1
change_particlesource [Actor 9] 1 6 {0 0 0} {0 0 0} 56 5 0 5 0 1
change_particlesource [Actor 10] 1 6 {0 0 0} {0 0 0} 56 5 0 5 0 1
change_particlesource [Actor 11] 1 6 {0 0 0} {0 0 0} 56 5 0 5 0 1
change_particlesource [Actor 12] 1 6 {0 0 0} {0 0 0} 56 5 0 5 0 1

set_particlesource [Actor 7] 1 1
set_particlesource [Actor 8] 1 1
set_particlesource [Actor 9] 1 1
set_particlesource [Actor 10] 1 1
set_particlesource [Actor 11] 1 1


sq_actor actionlist 7 {{{anim troll.verwesen_a}{anim troll.verwesen_a}} loop}
do_action anim troll.verwesen_a 7
sq_actor actionlist 8 {{{anim troll.verwesen_c}{anim troll.verwesen_c}} loop}
do_action anim troll.verwesen_c 8
sq_actor actionlist 9 {{{anim troll.verwesen_c}{anim troll.verwesen_c}} loop}
do_action anim troll.verwesen_c 9
sq_actor actionlist 10 {{{anim troll.verwesen_a}{anim troll.verwesen_a}} loop}
do_action anim troll.verwesen_a 10
sq_actor actionlist 11 {{{anim troll.verwesen_d}{anim troll.verwesen_d}} loop}
do_action anim troll.verwesen_d 11
sq_actor actionlist 12 {{{anim troll.verwesen_b}{anim troll.verwesen_b}} loop}
set_roty [Actor 12] 1.7
do_action anim troll.verwesen_b 12
sq_actor idleanim 1 drache.toetlich_d_stumm
do_action anim drache.toetlich_d_stumm 1
set_collision [Actor 1] 0

change_particlesource [Actor 1] 30 8 {0 0 0} {0 0 0} 156 20 0 10 0 1
set_particlesource [Actor 1] 30 1


#######################################################

#12/14
#sq_object summon Krumsaebel WPos
#13/15
#sq_object summon Schwert WPos
#link_obj [Object 6] [Actor 1] 8
#link_obj [Object 7] [Actor 1] 9


#######################################################
#Drache
set_textureanimation [Actor 1] 0 2
set_textureanimation [Actor 1] 1 2
#Trolle
set_textureanimation [Actor 7] 0 5
set_textureanimation [Actor 7] 1 5

set_textureanimation [Actor 8] 0 5
set_textureanimation [Actor 8] 1 5

set_textureanimation [Actor 9] 0 5
set_textureanimation [Actor 9] 1 5

set_textureanimation [Actor 10] 0 5
set_textureanimation [Actor 10] 1 5

set_textureanimation [Actor 11] 0 5
set_textureanimation [Actor 11] 1 5

set_textureanimation [Actor 12] 0 5
set_textureanimation [Actor 12] 1 5

sq_wait none
+sq_pen set Cam5Pos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move Cam5Pos {-26 0 0}
#sq_pen move Cam4Pos {15 0 0}
sq_camera fix Cam5Pos 0.7 -0.2 0.1
+sq_pen set ZwergTalkPos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move ZwergTalkPos {-23.3 0 2}
+do_action run ZwergTalkPos 0
+sq_pen set ZwergTalk2Pos [Getobjpos Info_Drache_Waypoint2]
+sq_pen move ZwergTalk2Pos {-22 0 4}
change_particlesource [Actor 1] 30 8 {0 0 0} {0 0 0} 156 20 0 10 0 1
+start_fade 5 1
do_wait time 4
###
sq_camera move Cam4Pos 1.3 -0.3 0.4 0.01
#sq_camera follow 0 1.5

do_wait time 13
#sq_wait 0
do_action anim scratchhead 0
do_wait time 2
do_action anim mann.treten_hintern 0
do_wait time 0.2
change_particlesource [Actor 0] 7 8 {0 -0.1 -0.1} {0 -0.3 0} 255 16 0 0 0 2
set_particlesource [Actor 0] 7 1
do_wait time 0.5
set_particlesource [Actor 0] 7 0
do_wait time 0.1

do_wait time 0.05

do_wait time 0.1

do_wait time 0.03


sq_wait 0
+do_action run ZwergTalk2Pos 0
+do_action rotate 3.7 0
do_wait time 12





+sq_pen set DragonDiePos 1
+sq_pen move DragonDiePos {-4 0 0}
sq_camera move DragonDiePos 1.4 -0.4 0.4 0.02
sq_wait 1

sq_actor idleanim 1 drache.aufrichten_d_warten

do_text 116a 1 {drache.toetlich_zu_aufrichten drache.aufrichten_d_sprechen drache.aufrichten_d_sprechen drache.aufrichten_d_sprechen drache.aufrichten_d_sprechen drache.aufrichten_d_sprechen} Ich_habe

#sq_actor actionlist 1 { {anim drache.liegen_d_stumm} loop }

#set_anim [Actor 1] drache.liegen_d_stumm 0 2 0

#link_obj [Object 12] [Actor 1] 10
do_wait time 1
change_particlesource [Actor 1] 30 8 {0 0 0} {0 0 0} 156 20 0 10 0 1
do_wait time 1
global richtung;set richtung [get_roty [Actor 0]]
+set_roty [Actor 0] 0
sq_actor eyes 0 {8}
sq_actor mouth 0 {7}
sq_camera fix 0 0.7 0.0 0.0
do_text 116b 0 Auto Mach_uns
do_wait time 4
global richtung;set_roty [Actor 0] $richtung
+sq_pen move DragonDiePos {-3 0 0}
sq_camera fix DragonDiePos 1.2 -0.4 0.4
change_particlesource [Actor 1] 30 8 {0 0 0} {0 0 0} 156 20 0 10 0 1
do_text 116c 1 {drache.aufrichten_d_sprechen drache.aufrichten_d_sprechen drache.aufrichten_d_sprechen drache.aufrichten_d_sprechen drache.aufrichten_d_sprechen drache.aufrichten_d_sprechen} Eins_noch
do_wait time 2
#do_text 116d 1 {drache.liegen_d_sprechen drache.liegen_d_sprechen} Sohn_kuemmern
do_wait time 2
-do_text 116e 0 Auto Sohn_kuemmern
do_wait time 4
+global richtung;set richtung [get_roty [Actor 0]]
+set_roty [Actor 0] 0
sq_actor eyes 0 {8}
sq_actor mouth 0 {7}
sq_camera fix 0 0.7 0.0 0.0
do_text 116f 0 Auto Natuerlich
do_wait time 7
+global richtung;set_roty [Actor 0] $richtung
sq_camera fix DragonDiePos 1.2 -0.4 0.4
sq_pen set EiPos 1
sq_pen move EiPos {-1 0 0}
sq_actor idleanim 1 drache.toetlich_d_stumm
do_text 116g 1 {drache.aufrichten_zu_tot} Argh
do_wait time 1
change_particlesource [Actor 1] 30 8 {0 0 0} {0 0 0} 156 20 0 10 0 1
do_wait time 3
do_text 116h 0 Auto Ach_du
do_wait time 5
#Falls Kacke wieder raus !
sq_color 0 {1 0 0}
do_text 116i 0 Auto Walhalla
#sq_sound Walhalla
do_wait time 0.2
+adaptive_sound changethemenow metall
+adaptive_sound primary metall

+sq_object summon Drachen_Ei EiPos
#Drache 1
change_particlesource [Actor 1] 20 13 {-4 -2.5 1} {0 0 0} 156 20 0 0 0 1
set_particlesource [Actor 1] 20 1

#Zwerg 1
change_particlesource [Actor 0] 20 13 {-1 -1.5 0} {0 0 0} 156 16 0 0 0 1
set_particlesource [Actor 0] 20 1
#Zwerg 2
change_particlesource [Actor 0] 21 13 {-2 -1.5 2} {0 0 0} 156 16 0 0 0 1
set_particlesource [Actor 0] 21 1
#Zwerg 2
change_particlesource [Actor 0] 22 13 {-4 -1.5 2} {0 0 0} 156 16 0 0 0 1
set_particlesource [Actor 0] 22 1
#Zwerg 3
change_particlesource [Actor 0] 23 13 {-4 -1.5 4} {0 0 0} 156 16 0 0 0 1
set_particlesource [Actor 0] 23 1
#Zwerg 4
change_particlesource [Actor 0] 24 13 {-6 -2.5 2} {0 0 0} 156 16 0 0 0 1
set_particlesource [Actor 0] 24 1

gametime factor 0.3
do_action anim drache.tot_walhalla 1
set_particlesource [Actor 1] 1 0


do_wait time 1
set_particlesource [Actor 1] 20 0
#Zwerg
set_particlesource [Actor 0] 20 0
set_particlesource [Actor 0] 21 0
set_particlesource [Actor 0] 22 0
set_particlesource [Actor 0] 23 0
set_particlesource [Actor 0] 24 0
do_wait time 1
+del [obj_query 0 -class Drache]

+gametime factor 1.0
+free_particlesource [Actor 0] 1 1
+free_particlesource [Actor 7] 1 1
+free_particlesource [Actor 8] 1 1
+free_particlesource [Actor 9] 1 1
+free_particlesource [Actor 10] 1 1


+del [Actor 7]
+del [Actor 8]
+del [Actor 9]
+del [Actor 10]
+del [Actor 11]
+del [Actor 12]
######################################################
#+set_collision [Actor 1] 1
+free_particlesource [Actor 0] 1 1
+free_particlesource [Actor 7] 1 1
+free_particlesource [Actor 8] 1 1
+free_particlesource [Actor 9] 1 1
+free_particlesource [Actor 10] 1 1

+free_particlesource [Actor 0] 20 1
+free_particlesource [Actor 0] 21 1
+free_particlesource [Actor 0] 22 1
+free_particlesource [Actor 0] 23 1
+free_particlesource [Actor 0] 24 1

+free_particlesource [Actor 1] 1 1
+free_particlesource [Actor 1] 30 1


