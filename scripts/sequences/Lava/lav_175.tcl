#Drachenbaby-Variante
sq_audio open 0175
sq_text file Lava

+sq_actor find Drache
sq_color 1 red

+sq_pen set DrachenPos 1
+sq_pen move DrachenPos {3 -2.2 0}

+sq_pen set DrachenKopfPos 1
+sq_pen move DrachenKopfPos {-2 -5.5 -11}

+sq_pen set DrachenTrauerPos 1
+sq_pen move DrachenTrauerPos {-1.5 -1.0 -11}

+sq_pen set WigglePos 1
+sq_pen move WigglePos {7 0 2}

+sq_pen set EiPos 1
+sq_pen move EiPos {0 0 6}

+sq_pen set DP 1
+sq_pen move DP {1 -2 0}

#############################################
sq_wait all
sq_color 1 Drache
sq_camera follow 0 1.0
do_action run WigglePos 0
sq_camera move DrachenPos 1.3 -0.2 -0.2
do_wait time 1
sq_camera fix DP 1.3 -0.2 -0.2
do_text 175a 1 {drache.sitzen_zu_schulter_l_d } Halt_wer
sq_pen move DP {-1 0 0}
sq_camera selset inout
sq_camera move DP 1.0 0.1 -0.6 0.5
do_text 175b 1 {drache.schulter_l_d_warten drache.schulter_l_d_warten drache.schulter_l_d_warten drache.schulter_l_d_warten} Ich_bin
sq_actor focus 0 1
sq_camera fix 0 1.0 -0.2 0.4
do_action anim shock 0
-sound play fe_schritt1 1
do_action anim mann.angst_start 0
do_action anim mann.angst_loop 0
-sound play fe_schritt2 1
do_action anim mann.angst_loop 0
do_action anim mann.angst_loop 0
-sound play fe_schritt1 1
sq_actor idleanim 0 mann.angst_loop
do_text 175c 0 {mann.angst_loop mann.angst_loop mann.angst_loop mann.angst_loop mann.angst_loop mann.angst_loop mann.angst_loop mann.angst_loop mann.angst_loop} Wir
+set_roty [Actor 1] 4.71
sq_camera fix DrachenKopfPos 0.65 0.2 -0.8
do_text 175d 1 {drache.sitzen_dialog_d drache.sitzen_dialog_d drache.sitzen_dialog_g} Ich_habe
do_wait time 2
sq_camera fix 0 0.65 -0.4 0.8
do_action anim mann.angst_end 0
do_action anim breathe 0
sq_actor idleanim 0 mann.stand_anim_a

do_text 175e 1 {drache.sitzen_dialog_d} Wie_gehts
do_wait time 2
#do_wait time 1
sq_actor focus 0 1
#do_action anim scratchhead 0
#do_action anim teeter_t 0
#do_text "Natürlich, Natürlich !" 0
sq_actor eyes 0 { u u u u u}
sq_actor eyes 0 {c c 9 c c c c 9 c c}
do_wait time 2
sq_actor eyes 0 { u u u u u}
#do_action anim teeter_t 0
sq_actor eyes 0 {c c 9 c c c c 9 c c}
do_wait time 1
sq_actor focus 0 1
do_text 175f 0 {teeter_t scratchhead teeter_t scratchhead teeter_t breathe wait mann.dialog_ac_negativ_b mann.dialog_mittel_antwort mann.dialog_re_positiv_a } Nun_ja
sq_camera fix DrachenTrauerPos 1.25 -0.2 -0.8
sq_wait none
do_action anim drache.sitzen_umdrehen 1
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt2 1
do_wait time 1
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt1 1
do_wait time 1
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt2 1
do_wait time 1
#do_wait time 3.0
sq_actor eyes 0 {10}
sq_camera fix 0 0.7 -0.2 0.7
set_roty [Actor 1] 1.57
-screenvibe 0 0.1 0.2 0 0 0.2 100
-sound play fe_schritt2 1
do_wait time 1
do_text 175g 1 Auto Schnueff
do_wait time 2
sq_camera fix DrachenTrauerPos 1.25 -0.2 -0.8
sq_actor eyes 0 {0}
sq_actor focus 0 1
sq_wait all
do_text 175h 1 Auto Ein_Schwaetzer
sq_camera fix 0 0.65 -0.4 0.8
do_text 175i 0 Auto Wir_haben

##################Ei oder Drache ###################
sq_pen set WigglePlusPos 0
sq_pen move WigglePlusPos {-2 0 0}

#sq_camera fix EiPos 1.2 -0.2 -0.2
sq_pen move DrachenPos {-3.5 0.3 0}
do_wait camera
sq_camera move DrachenPos 1.4 -0.2 -0.2
do_action run WigglePlusPos 0
sq_actor find Drachenbaby
sq_wait 2
sq_pen set BabyPlusPos 1
sq_pen move BabyPlusPos {7 0 4}
do_action beam BabyPlusPos 2
sq_pen set EiPutPos 1
sq_pen move EiPutPos {-0.5 0 4.5}
do_action walk Eibackpos 0
do_action run EiPutPos 2
sq_pen set EiBackPos EiPos
sq_pen move EiBackPos {2 0 2}
do_action walk EiBackPos 0
do_action rotate left 0
#do_wait time 1
#sq_pen move DrachenPos {-1 1 8}
#sq_camera move DrachenPos 1.3 -0.2 -0.2
sq_wait none
sq_actor actionlist 2 { {anim drache01.sitzen_knuddeln} loop }
do_action anim drache01.sitzen_wedeln 2
sq_wait 0
sq_wait 1
do_text 175j 1 {drache.mama_sitzen_knuddeln_start drache.mama_sitzen_knuddeln_loop drache.mama_sitzen_knuddeln_loop drache.mama_sitzen_knuddeln_loop drache.mama_sitzen_knuddeln_loop drache.mama_sitzen_knuddeln_loop} Mein_Sohn
do_action anim drache.mama_sitzen_knuddeln_loop 1
sq_wait none
sq_wait 0
do_action anim scratchhead 0
sq_actor actionlist 1 {}
sq_camera follow 0 1.4
sq_actor actionlist 2 {}
sq_actor actionlist 1 { {anim drache.mama_sitzen_knuddeln_loop } loop }
do_action anim drache.mama_sitzen_knuddeln_loop 1
do_action walk WigglePos 0
sq_camera fix DrachenPos 1.4 -0.2 -0.2
sq_actor actionlist 1 {}
sq_wait none
sq_wait 1
do_text 175k 1 {drache.mama_sitzen_knuddeln_sprechen  drache.mama_sitzen_knuddeln_sprechen drache.mama_sitzen_knuddeln_sprechen drache.mama_sitzen_knuddeln_end2} Wartet_ihr
sq_actor find Lava_Hammer_Stein

sq_camera fix 3 1.0 -0.2 0.2
do_wait time 0.1
sq_pen set BrennerPos 3
sq_pen move BrennerPos {2 0 0}
sq_pen set HammerCam 3
sq_pen move HammerCam {1 0 -4}
sq_camera move HammerCam 1.2 -0.3 -0.5 0.05
do_wait time 1.0
sq_camera selset inout
call_method [Actor 1] set_enemy [Actor 3]
do_action anim drache.sitzen_zu_pumpe_start 1
call_method [Actor 1] fire2_start
do_wait time 0.5
change_particlesource [Actor 3] 3 34 {-0.5 0.4 1.5} {0 -0.1 0.7} 106 4 0 0 0 1
set_particlesource [Actor 3] 3 1
change_particlesource [Actor 3] 4 34 {0.4 0.4 1.5} {0 -0.1 0.7} 106 4 0 0 0 1
set_particlesource [Actor 3] 4 1
change_particlesource [Actor 3] 5 34 {-1 0 -1} {0 -0.1 0.2} 106 4 0 0 0 1
set_particlesource [Actor 3] 5 1
change_particlesource [Actor 3] 6 35 {0 -2 -1.0} {0 0 0} 106 4 0 0 0 1
set_particlesource [Actor 3] 6 1
do_action anim drache.sitzen_zu_pumpe_loop 1

#sq_camera move HammerCam 1.2 -0.3 -0.5 0.05
do_action anim drache.sitzen_zu_pumpe_loop 1
sq_pen set NeuHammerPos 3
+sq_object summon GleipnirHammer NeuHammerPos
+sq_object summon Stein NeuHammerPos
sq_actor find GleipnirHammer
sq_actor find Stein

set_visibility [Actor 5 ] 0
set_rotz [Object 0] 0
set_physic [Object 0] 0

sq_pen set HamPos 4
sq_pen move HamPos {0 -0.3 0}

sq_object beam 0 HamPos


#4
change_particlesource [Actor 5] 3 34 {0 0.4 0} {0 -0.1 0.7} 106 4 0 0 0 1
set_particlesource [Actor 5] 3 1
change_particlesource [Actor 5] 4 34 {0.4 0.4 0} {0 -0.1 0.7} 106 4 0 0 0 1
set_particlesource [Actor 5] 4 1
change_particlesource [Actor 5] 5 34 {0 0 0} {0 -0.1 0.2} 106 4 0 0 0 1
set_particlesource [Actor 5] 5 1
change_particlesource [Actor 5] 6 35 {0 0 -0.5} {0 0 0} 106 4 0 0 0 1
set_particlesource [Actor 5] 6 1

do_action anim drache.sitzen_zu_pumpe_loop 1
do_action anim drache.sitzen_zu_pumpe_loop 1
set_visibility [obj_query 0 -class Lava_Hammer_Stein  ] 0
+del [obj_query 0 -class Lava_Hammer_Stein  ]
do_action anim drache.pumpe_zu_sitzen_loop 1


do_wait time 1
call_method [Actor 1] fire_stop
do_wait time 0.5
set_particlesource [Actor 5] 3 0
set_particlesource [Actor 5] 4 0
set_particlesource [Actor 5] 5 0
set_particlesource [Actor 5] 6 0
do_wait time 2
do_action anim drache.pumpe_zu_sitzen_end 1
do_wait time 2
sq_camera fix DrachenPos 1.3 -0.2 -0.2
+del [Actor 5]
do_text 175l 1 {drache.mama_sitzen_knuddeln_start drache.mama_sitzen_knuddeln drache.mama_sitzen_knuddeln_end drache.mama_sitzen_knuddeln_sprechen drache.mama_sitzen_knuddeln_sprechen drache.mama_sitzen_knuddeln_end2} Das_ist
do_wait time 2
sq_pen move HammerCam {-1 0 0}
sq_camera move HammerCam 0.8 -0.3 -0.5 0.5
do_wait time 3
#sq_camera follow 0 1.2
do_wait time 1

#+free_particlesource [Actor 3] 1 1
#+free_particlesource [Actor 3] 2 1
#+free_particlesource [Actor 3] 3 1
#+free_particlesource [Actor 3] 4 1

#+sq_object delete all

#Drachenmama
