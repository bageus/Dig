sq_text file Urwald
sq_audio open Clip_20
#-------------------------------------------------------------------------------------------------------
+sq_actor find Einsiedler 100 1 any
+set_anim /obj/Einsiedler mann.sitzen_sofa_loop 0 2

#sq_color 0 White
#sq_color 1 Yellow
+sq_pen set ZBack 0
+sq_pen set cam2 TriggerPos
+sq_pen set cam2a TriggerPos
+sq_pen set cam2b TriggerPos
+sq_pen set cam2c TriggerPos
+sq_pen set cam3 TriggerPos
+sq_pen set cam4 TriggerPos
+sq_pen set cam7 TriggerPos
+sq_pen set cam8 TriggerPos
+sq_pen set cam9 TriggerPos
+sq_pen set cam9b TriggerPos
+sq_pen set cam10 TriggerPos
+sq_pen set cam15 TriggerPos
+sq_pen set elfKamera 0
+sq_pen set eins1 1
+sq_pen set eins1b 1
+sq_pen set eins2 1
+sq_pen set eins3 1
+sq_pen set eins4 1
+sq_pen set einsB 1
+sq_pen move cam2 {-8.5 0 -2}
+sq_pen move cam2a {-9.5 0 -2};#8.5 statt -9.5
+sq_pen move cam2b {-12 0 -2}
+sq_pen move cam2c {-13 0 -1}
+sq_pen move cam3 {-10 0 -1}
+sq_pen move cam4 {-5.5 0 1}
+sq_pen move cam7 {-11 0 -2}
+sq_pen move cam8 {-5 0 -1.5}
+sq_pen move cam9 {-7 0.5 -4}
+sq_pen move cam9b {-0.0 0.5 -1}
+sq_pen move cam10 {-11 0 -1}
+sq_pen move cam15 {-12.3 0 -2.0} ;# 1.0
#+sq_pen move eins1 {-3.5 0.1 1}
+sq_pen move eins1 {-1.0 0.1 1}
+sq_pen move eins1b {-3.5 0.1 1}
+sq_pen move eins2 {0.1 0 1}
+sq_pen move eins3 {0 0 0.5}
+sq_pen move einsB {0.4 0.0 -0.8}
#+sq_pen move elfKamera { 1 -0.6 2.8}
+sq_pen move elfKamera { -1.6 -0.1 2.8}
+sq_pen set elfStartPos 0
+sq_pen move elfStartPos {24 0 0}
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }

#-------------------------------------------------------------------------------------------------------
do_action rotate left 0
do_action rotate left 1
do_elf beam elfStartPos
sq_camera selset inout
sq_camera move 0 1 0 0.4 0.5
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_wait all
#-------------------------------------------------------------------------------------------------------
adaptive_sound markerdisable
adaptive_sound changethemenow einsiedlerauftrag
#-Wiggle:		Hmmm... Hier riechts irgendwie... verbrannt!
do_text 022a 0 {{sniffatfood} {sniffatfood}} Riecht

sq_wait none
#-Wiggle:		Kommmt von da, glaub ich...
do_text 022b 0 {{scout} {talkacntc}} Kommt
do_elf path elfStartPos elfKamera

#-------------------------------------------------------------------------------------------------------
do_wait time 4

#Riecht nach Hamster-Barbeque... Oder doch gegrillen Wiggles?! Hihi... Ich seh mal nach.
#-------------------------------------------------------------------------------------------------------
#sq_actor actionlist 0 { {breathe} {scratchhead} {handstandstart} {handstandloop}}
#do_action anim handstandstop 0
#sq_sound Ich 0
do_elf text 022ba {reden_a|kichern|reden_b} Ich
sq_actor actionlist 0 {{anim handstandstart} {anim handstandloop} {anim handstandloop} {anim handstandstop}}
do_action anim breathe 0
do_wait time 6

do_elf move cam8
do_wait time 1.2
do_action anim scratchhead 0
do_wait time 1.1
+do_elf hide

#_______________Schnitt in der Bewegung__________
sq_wait none
do_action run cam8 0
do_wait time 1.4

sq_camera fix cam9 1.3 -0.3 -0.6
sq_wait all

do_action rotate 2.2 0
#-------------------------------------------------------------------------------------------------------
#-Wiggle:		Bei Odin!
#-------------------------------------------------------------------------------------------------------
#do_wait time 0.3
do_action run cam4 0
#do_wait time 1.0
do_action anim shock 0
#do_wait time 0.3
do_action run cam2 0

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }
sq_camera fix 0 0.8 -0.3 0.5
#do_action rotate right 1
do_action rotate back 0
do_action anim scratchhead 0
sq_wait 0
sq_camera selset inout
sq_camera move 1 1 -0.3 -0.3 0.2
#do_wait camera
#-------------------------------------------------------------------------------------------------------
#-Einsiedler:
#do_text "Alles zerstört haben Sie! Kaputt ist alles! Vandalen sie sind! (Schnief) Aufknöpfen man sie alle müsste. UAAA...." 1 {{getcomfort} {breathe} {getcomfort} {getcomfort} {breathe}} Alles
do_text 020a 1 {{getcomfort} {breathe} {getcomfort} {getcomfort} {mann.unterhalten_n} {getcomfort} {talkrentb} {getcomfort} {standloopa}} Alles
#-------------------------------------------------------------------------------------------------------
do_action rotate left 0
do_wait time 9
do_action run cam15 0

do_wait time 2
#-------------------------------------------------------------------------------------------------------
#-Wiggle:		Alter! Hier riechts voll nach gegrilltem Hamster!
do_text 020b 0 {sniffatfood} Alter
#-------------------------------------------------------------------------------------------------------

sq_camera fix cam15 0.9 -0.1 0.35 ;#0.9 -0.1 0.5
#do_ation rotate 1 0

#do_wait time 1

#sq_actor actionlist 0 {{anim wipenose} {anim wait}}
#do_action anim wait 0

do_action rotate right 1
do_wait time 0.8
#-------------------------------------------------------------------------------------------------------
#-Einsiedler:
#do_text "Lustig machen, das kannst Du Dich!" 1 { {kungfufistmiddle} } Mach;#{kungfufistmiddle}
do_text 020c 1 { {breathe} {talkacntc} {getcomfort} {talkrentb} {getcomfort} } Lustig

#-------------------------------------------------------------------------------------------------------
do_wait time 1.7
#-------------------------------------------------------------------------------------------------------
#-Wiggle:		Diese Dumpfbacken!... Diese Trolle!...|Die waren hier - haben alles kaputt gemacht, alles...
#do_text "Dumpfbacken diese Trolle sind.|Alles kaputtgemacht haben Sie!" 1 {{breathe} {getcomfort} {breathe} {getcomfort}} Diese
#do_text 020d 1 {{breathe} {getcomfort} {breathe} {getcomfort}} Die

#-------------------------------------------------------------------------------------------------------
do_action anim scratchhead 0
do_wait time 5
+sq_pen set cam11 1
+sq_pen move cam11 {0.5 0 0}
sq_camera fix cam11 0.7 0 -0.1 0.3
sq_wait none

do_action rotate left 0
do_wait time 0.5
sq_actor eyes 1 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 0 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }
#-------------------------------------------------------------------------------------------------------
#-Wiggle:		Beruhig dich, alter Zwerg - ja?
do_text 020e 0 {tocomfort} Beruh

do_wait time 0.8
#-------------------------------------------------------------------------------------------------------
do_action rotate right 1
do_wait time 1
#-------------------------------------------------------------------------------------------------------
#do_text "Beruhigen? BERUHIGEN ICH SOLL?!?" 1 {{impatient} {talkpangc}} Beruhig
do_text 020f 1 {{breathe} {talkpangc}} Beruhigen
#-------------------------------------------------------------------------------------------------------
do_wait time 1
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }
+sq_pen set cam13 1
+sq_pen move cam13 {-3 -0.5 2}
sq_camera selset inout
sq_camera move cam13 0.75 -0.2 1.0 0.65
#	zoom x y speed
do_wait time 0.5
do_action walk eins1 1
do_wait time 1.5
do_action rotate left 1
do_wait time 0.4

#-------------------------------------------------------------------------------------------------------
#-Einsiedler:	Meine ganzen Hamster - alle weg! Verbrannt oder weggelaufen! |Wie soll ich jetzt über'n Winter kommen.hmm!
#do_text "Meine Hamster  - alle verbrannt sie sind. Wie ich soll über den Winter kommen - du mir sagen?!" 1 {{cheer} {breathe} {breathe} {praystart} {prayloop} {prayloop} {prayloop} {praystop}} Meine

#sq_actor actionlist 1 {{anim breathe} {anim breathe} {anim praystart} {anim prayloop} {animprayloop} {anim praystop} {rotate rigth}}
#do_action anim cheer 1
#sq_wait 1
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }
do_text 020g 1 {{cheer} {praystart} {prayloop} {prayloop} {prayloop} {praystop} {breathe}} Meine
#-------------------------------------------------------------------------------------------------------
sq_actor actionlist 0 {{anim carvestart} {anim carveloop} {anim carveloop} {anim carveloop} {anim carveloop} {anim carveloop} {anim carveloop} {anim carveloop} {anim carveloop} {anim carvestop}}
do_action anim stretch 0
do_wait time 5;#8
sq_wait none
+sq_pen set cam12 0
+sq_pen move cam12 {-1.4 0 0}
sq_camera fix cam12 0.8 0 -0.2
do_wait time 2

#-------------------------------------------------------------------------------------------------------
#-Wiggle:	Keine Ahnung!!
#do-text "Hm - Keine Ahnung." 0 {dontknow} Keine
do_action rotate right 1
do_wait time 0.3
do_text 020h 0 {dontknow} Nein

#-------------------------------------------------------------------------------------------------------
do_wait time 2.0
do_wait camera
do_action rotate right 1
#do_wait time 2
do_action walk eins2 1
+sq_pen move cam12 {1 0 0}
sq_camera selset inout
sq_camera move cam12 0.8 0 -0.3 0.4
do_action rotate right 0
do_wait time 1
do_action anim scout 0
do_action rotate right 1
#-------------------------------------------------------------------------------------------------------
#-Einsiedler:
#-Einsieder: Helfen mir du mußt! Du mußt, Hörst du! Daß du mir hilfs sage es mir?
do_text 020i 1 {{talkpanta} {breathe} {teeter_t} {bowllose} {breathe}} Helfen
#-------------------------------------------------------------------------------------------------------
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }
do_wait time 3;#4
do_action rotate left 0
do_wait time 2
do_action anim talkpantb 0
do_wait time 1.5
+sq_pen set eins4 1
+sq_pen move eins4 {-1.5 0 2}
+sq_pen set cam14 1
+sq_pen move cam14 {-1 0 -2}
sq_camera fix 0 0.65 -0.2 -1.0

do_wait time 0.2
sq_wait none
#-------------------------------------------------------------------------------------------------------
#-Einsiedler:
#do_text "Bitte! Meine Hamster wieder einfangen Du musst, junger Wiggle! Oder mir schenken ein paar Du sollst." 1 {{mann.einsiedler_a} {praystop}} Bitte
do_text 020j 1 {{mann.einsiedler_a} {praystop} {talkacntc} {talkacntb} {talkacnta}} Bitte
#-------------------------------------------------------------------------------------------------------
do_wait time 2.25
do_action rotate left 0
do_wait time 0.4
do_action anim scratchhead 0
do_wait time 0.7
#do_action walk cam15 0
sq_camera fix cam11 0.8 0 -0.1 -0.5
do_wait time 2
do_action rotate left 0
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }
do_wait time 4.0;#3.5

#-------------------------------------------------------------------------------------------------------
#-Einsiedler:
#do_text "Die Torwächterin befreien Du willst? Den Weg zu Ihr weisen, das werde ich. Unbemerkt in die Festung kommen Du kannst. Groß machen Kriege niemanden." 1 {{dontknow} {talkacngb} {talkacnta} {dontknow} {talkacntb}} Bekommst
do_text 020k 1 {{talkrentb} {talkpanta} {talkacntc} {talkrentb} {talkacntb} {talkacnta} {talkacntb} {standloopa} {talkrengb} {talkacngc}} Die
#-------------------------------------------------------------------------------------------------------
sq_actor actionlist 0 {{anim listenastart} {anim listenaloop} {anim listenaloop} {anim listenaloop} {anim listenastop}}
do_action anim cough 0
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }
do_wait time 4.5;#6.5
sq_camera fix cam11 0.7 -0.2 1.0
do_action rotate left 0
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }
do_wait time 4.5;#8.5
#sq_wait none
#-------------------------------------------------------------------------------------------------------
#-Wiggle:Und Was ...
#do_text "Und was ..." 0 {scratchhead} UndWas;#020l
#-------------------------------------------------------------------------------------------------------
#do_wait time 0.5
do_action anim breathe 0
do_wait time 0.5
#-------------------------------------------------------------------------------------------------------
#-Einsiedler:	PSSSTTT...
#-------------------------------------------------------------------------------------------------------
#do_text 020m 1 {wipenose} Pssst
#do_action anim leftright 1
#do_action anim scratchhead 0
#do_wait time 0.5
#do_action anim scratchhead 0
#do_wait time 2.5
#-------------------------------------------------------------------------------------------------------
#-Einsiedler:	Hmmm... Also, wenn du mir 6 Hamster beschaffst, sag ichs dir.
#do_text "Ungeduldig Du bist, junger Wiggel, wie Dein Vater. Erst wenn Du sechs Hamster bringst, dann bereit Du bist." 1 Auto Hmmm

sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c c c c c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c }
do_text 020n 1 Auto Ungeduldig
#-------------------------------------------------------------------------------------------------------
sq_camera selset inout
sq_camera move 1 1.0 -0.2 -0.3 0.5
sq_wait camera
do_wait time 8.5

+do_action beam cam15 0
sq_camera fix 1 1.0 -0.2 -0.3;# eins4 -1 x
do_wait time 1
sq_wait all
do_action walk einsB 1
+do_action beam einsB 1
do_action rotate 5.4 1
do_action anim couchstart 1
+adaptive_sound markerenable
+adaptive_sound changethemenow cave
+set_anim /obj/Einsiedler mann.sitzen_sofa_loop 0 2
+sq_camera fix einsB 1.0 -0.2 -0.3
#+sq_camera get
+set_roty /obj/Einsiedler 5.4
+sq_sound Nichts 0
+do_wait

