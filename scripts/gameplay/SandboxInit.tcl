# unique templates and story events definition:
call data/scripts/init/lgtools.tcl

//sm_reset
sm_create_map 512 640

sm_set_digcount 44

// Story Events
sm_add_event Uebergang_Urw_Met
sm_add_event Uebergang_Met_Kris
sm_add_event Uebergang_Kris_Lava

// Kampagnen Definition
sm_add_zone "Urwald"
sm_add_zone "Metall"
sm_add_zone "Kristall"
sm_add_zone "Lava"

// Newsticker Meldungen

//{{KFX_MSG_MAP(CStoryMgr)
lappend newslist {{dig}	{dig dig}}		;# Dig Messages
//}}KFX_MSG_MAP
