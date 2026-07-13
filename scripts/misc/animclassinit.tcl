;# animclassinit.tcl
;# bekanntgeben der definitionen aus animinit.tcl fuer alle anderen scripte

# call scripts/init/animinit.tcl
if {[in_class_def]} {
set ANIM_STILL 	0	;# spiele keine animation ab
set ANIM_ONCE 	1	;# spiele die animation genau einmal ab
set ANIM_LOOP 	2	;# spiele die animation staendig ab

;# definition der animationsdatenbanken

member ANIM_STILL ANIM_ONCE ANIM_LOOP

}

