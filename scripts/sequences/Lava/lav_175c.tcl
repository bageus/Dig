do_wait camera
sq_actor find Zwerg 20 1 0
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s175c
#-----------------------------------------
sq_wait all
##########################
sq_pen set HammerPos [Getobjpos Lava_Hammer_Stein]

sq_pen set HammerPos2 [Getobjpos Lava_Hammer_Stein]
sq_pen move HammerPos2 {1 0 0}

sq_pen set WigglePos HammerPos
sq_pen move WigglePos {2 0 2}

sq_pen set WigWorkPos HammerPos
sq_pen move WigWorkPos {1.2 0 1.5}

sq_pen set PressluftPos HammerPos
sq_pen move Pressluft {1.0 0 1.5}

##########################
sq_camera fix HammerPos 1.0 -0.2 -0.6

do_action walk WigglePos 0
do_action rotate HammerPos 0
do_wait time 3
sq_camera selset inout
sq_camera move HammerPos 0.9 -0.1 -0.5 0.4
do_wait time 1
do_action walk WigWorkPos 0
do_tooltakeout Axt 0
do_action anim digfront 0
do_action anim digfront 0
do_action anim digfront 0
do_action anim digdownaccidentstone 0
#do_text "Hmpf !" 0
do_toolputaway 0
sq_camera fix HammerPos2 0.85 -0.1 0.8
do_wait time 2

do_tooltakeout Spitzhacke 0
sq_camera fix HammerPos 1.0 -0.2 -0.6
do_action anim digfront 0
do_action anim digfront 0
do_action anim digfront 0
sq_camera fix HammerPos2 0.85 -0.1 0.8
do_action anim digdownaccidentstone 0
do_toolputaway 0
do_wait time 2
#do_action beam PressluftPos 0
do_action anim airhammwallstartb 0
do_action anim airhammfrontstart 0
do_action anim airhammfrontloop 0
do_action anim airhammfrontloop 0
do_action anim airhammwallf2d 0
do_action anim airhammdownaccident 0
do_action anim airhammdownaccident 0

set_roty [Actor 0] 1.0
do_action anim stummble 0
do_action anim standup 0
do_action walk WigWorkPos 0
do_wait time 1
do_action anim scratchhead 0
do_wait time 1
do_action rotate HammerPos 0
do_action anim sawvertikalstart 0
change_particlesource [Actor 0] 1 6 {-2.0 0 0} {0 0 0} 156 16 0 0 0 0
set_particlesource [Actor 0] 1 1
do_action anim sawvertikalloop 0
do_action anim sawvertikalloop 0
do_action anim sawvertikalloop 0
do_action anim sawvertikalloop 0
do_action anim sawvertikalend 0
+free_particlesource [Actor 0] 1 1
do_wait time 1
######################
start_fade 3 0
do_wait time 2
+del [obj_query 0 -class Lava_Hammer_Stein]
+sq_object summon GleipnirHammer HammerPos
do_action beam WigglePos 0
do_wait time 2
sq_wait none
start_fade 3 1
######################
sq_pen set Blitz1 0
sq_pen move Blitz1 {-1.0 -0.3 0}
sq_pen set Blitz2 HammerPos
sq_pen move Blitz2 {0 0.1 0}
sq_actor actionlist 0 { {anim laserdrillfrloop} loop}
do_action anim laserdrillfrloop 0
-lightning [parse_pos Blitz1] [parse_pos Blitz2] {0 0 0} 6.0 0.855 0.890 0.914
do_wait time 6
sq_actor actionlist 0 { }
do_wait time 1
do_action anim bowlwin 0
do_wait time 2
sq_camera follow 0 1.1
do_wait time 2
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow drachenhoehle
#-----------------------------------------

