call scripts/init/animinit.tcl

def_class Misc_Light none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_viewinfog 1
	class_light {0 -0.5 1.0} 6 {0.9 0.9 0.8}
	class_defaultanim fackel.standard
	class_particle 2 {0 -0.1 0.2} {0 0 0} 32 1 0 0 -1
	class_snaptowall 1
}

def_class Misc_LightSrc_a none dummy 0 {} {
	class_light {0 -0.5 0} 2 {0.6 0.6 0.5} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}

def_class Misc_LightSrc_b none dummy 0 {} {
	class_light {0 -0.5 0} 3 {0.6 0.6 0.5} -1
	class_disablescripting
	class_viewinfog 1	
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}

def_class Misc_LightSrc_c none dummy 0 {} {
	class_light {0 -0.5 0} 4 {0.6 0.6 0.5} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}

def_class Misc_LightSrc_d none dummy 0 {} {
	class_light {0 -0.5 0} 5 {0.6 0.6 0.5} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Misc_LightSrc_e none dummy 0 {} {
	class_light {0 -0.5 0} 6 {0.7 0.7 0.6} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Misc_LightSrc_f none dummy 0 {} {
	class_light {0 -0.5 0} 8 {0.7 0.7 0.6} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}




if { [minimalrun] } {return;}

def_class Dummy_Met_licht_a none dummy 0 {} {
	class_light {0 -1.3 0} 4 {0.5 0.6 0.15} 
	class_disablescripting
	class_viewinfog 1	
	class_defaultanim met_licht_a.standard
}

def_class Dummy_Met_licht_b none dummy 0 {} {
	class_light {0 -1.3 0} 2.3 {0.77 0.65 0}
	class_disablescripting
	class_viewinfog 1
	class_defaultanim met_licht_b.standard
}

def_class Dummy_Met_licht_c none dummy 0 {} {
	class_light {0 -1.3 0} 2.3 {0.77 0.65 0}
	class_disablescripting
	class_viewinfog 1
	class_defaultanim met_licht_c.standard
}

def_class Dummy_Met_licht_d none dummy 0 {} {
	class_light {0 0 2} 4 {0.9 0.8 0.7}
	class_disablescripting
	class_viewinfog 1
	class_defaultanim met_licht_d.standard
}

def_class Dummy_Met_licht_e none dummy 0 {} {
	class_light {0 0 2} 4 {0.9 0.8 0.7}
	class_disablescripting
	class_viewinfog 1	
	class_defaultanim met_licht_e.standard
}

def_class Dummy_Met_licht_g none dummy 0 {} {
	class_light {0 0 2} 4 {0.8 0.55 0.15}
	class_disablescripting
	class_viewinfog 1	
	class_defaultanim met_licht_g.standard
}

def_class Dummy_Met_licht_f none dummy 0 {} {
	class_light {0 0 2} 4 {0.9 0.8 0.7}
	class_disablescripting
	class_viewinfog 1
	class_defaultanim met_licht_f.standard
}

def_class Dummy_Met_licht_f1 none dummy 0 {} {
	class_light {0 -1.3 0} 2.3 {0.4 0.5 0.15}
	class_disablescripting
	class_viewinfog 1	
	class_defaultanim met_licht_a.standard
}

def_class Dummy_Met_licht_f2 none dummy 0 {} {
	class_light {0 -1.3 0} 2.3 {0.7 0.4 0.6}
	class_disablescripting
	class_viewinfog 1
	class_defaultanim met_licht_a.standard
}

def_class Dummy_Met_licht_f3 none dummy 0 {} {
	class_light {0 -1.3 0} 2.3 {0.2 0.5 0.9}
	class_disablescripting
	class_viewinfog 1	
	class_defaultanim met_licht_a.standard
}

def_class Misc_SchwefelSrc_a none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_viewinfog 1
	class_particle 9 {0 0 0} {0 0 0} 8 2 0 0 0
}

def_class S_Test none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_viewinfog 1
	class_defaultanim swf_krst_krat_a.standard
	class_particle 9 {0 0 0} {0 0 0} 8 2 0 0 0
}

def_class Misc_SchwefelSrc_b none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_viewinfog 1
	class_particle 9 {0 0 0} {0 0 0} 10 1 0 0 0
}

def_class Misc_SchwefelSrc_c none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_viewinfog 1	
	class_particle 9 {0 0 0} {0 0 0} 50 2 0 0 0
}

def_class Misc_DropSrc_c none dummy 0 {} {
	call scripts/misc/animclassinit.tcl
	class_disablescripting
	class_viewinfog 1
	class_particle 10 {0 0 0} {0 0 0} 5 1 0 0 0
}


def_class Kris_LightSrc_a none dummy 0 {} {
	class_light {0 0 0} 6 {0.28 0.65 0.60} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Kris_LightSrc_a1 none dummy 0 {} {
	class_light {0 0 0} 3 {0.28 0.65 0.60} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}

def_class Kris_LightSrc_b none dummy 0 {} {
	class_light {0 0 0} 6 {0.23 0.44 0.63} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}

def_class Kris_LightSrc_b1 none dummy 0 {} {
	class_light {0 0 0} 3 {0.23 0.44 0.63} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}

def_class Kris_LightSrc_c none dummy 0 {} {
	class_light {0 0 0} 6 {0.40 0.75 0.79} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}

def_class Kris_LightSrc_c1 none dummy 0 {} {
	class_light {0 0 0} 3 {0.40 0.75 0.79} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Kris_LightSrc_d none dummy 0 {} {
	class_light {0 0 0} 6 {0.02 0.95 0.93} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Kris_LightSrc_d1 none dummy 0 {} {
	class_light {0 0 0} 3 {0.02 0.85 0.83} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Kris_LightSrc_e none dummy 0 {} {
	class_light {0 0 0} 6 {0.56 1 0.45} -1
	class_disablescripting
	class_viewinfog 1	
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}

def_class Kris_LightSrc_f none dummy 0 {} {
	class_light {0 0 0} 6 {0 0 1} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Kris_LightSrc_g none dummy 0 {} {
	class_light {0 0 0} 6 {0 0 0.5} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Kris_LightSrc_g1 none dummy 0 {} {
	class_light {0 0 0} 6 {0 0 0.5} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_a none dummy 0 {} {
	class_light {0 0 0} 6 {0 0.29 0.5} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_b none dummy 0 {} {
	class_light {0 0 0} 6 {0.64 0.46 0.64} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_c none dummy 0 {} {
	class_light {0 0 0} 6 {0 0.45 0.42} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}





def_class Urw_LightSrc_d none dummy 0 {} {
	class_light {0 0 0} 6 {0 0.45 0.21} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_d1 none dummy 0 {} {
	class_light {0 0 0} 4 {0 0.45 0.21} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_d2 none dummy 0 {} {
	class_light {0 0 0} 2 {0 0.45 0.21} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_e none dummy 0 {} {
	class_light {0 0 0} 6 {0.22 0.71 0.29} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_e1 none dummy 0 {} {
	class_light {0 0 0} 4 {0.22 0.71 0.29} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_e2 none dummy 0 {} {
	class_light {0 0 0} 2 {0.22 0.71 0.29} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_f none dummy 0 {} {
	class_light {0 0 0} 6 {0.49 0.77 0.46} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_f1 none dummy 0 {} {
	class_light {0 0 0} 4 {0.49 0.77 0.46} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_f2 none dummy 0 {} {
	class_light {0 0 0} 2 {0.49 0.77 0.46} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Lava_LightSrc_red1 none dummy 0 {} {
	class_light {0 0 0} 5 {1 0 0} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}




def_class Lava_LightSrc_g none dummy 0 {} {
	class_light {0 0 0} 6 {0.98 0.53 0.03} -1
	class_disablescripting
	class_viewinfog 1	
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Lava_LightSrc_g1 none dummy 0 {} {
	class_light {0 0 0} 4 {0.98 0.53 0.03} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Lava_LightSrc_g2 none dummy 0 {} {
	class_light {0 0 0} 2 {0.98 0.53 0.03} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_h none dummy 0 {} {
	class_light {0 0 0} 6 {0.6 0.07 0.6} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_i none dummy 0 {} {
	class_light {0 0 0} 6 {0.26 0.59 0.58} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Swf_LightSrc_j none dummy 0 {} {
	class_light {0 0 0} 6 {1 1 0.33} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Swf_LightSrc_j1 none dummy 0 {} {
	class_light {0 0 0} 4 {1 1 0.33} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Swf_LightSrc_j2 none dummy 0 {} {
	class_light {0 0 0} 2 {1 1 0.33} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Swf_LightSrc_k none dummy 0 {} {
	class_light {0 0 0} 6 {0.92 0.84 0.4} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Swf_LightSrc_k1 none dummy 0 {} {
	class_light {0 0 0} 4 {0.92 0.84 0.4} -1
	class_disablescripting
	class_viewinfog 1	
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Swf_LightSrc_k2 none dummy 0 {} {
	class_light {0 0 0} 2 {0.92 0.84 0.4} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_l none dummy 0 {} {
	class_light {0 0 0} 6 {0.49 0.65 0.85} -1
	class_disablescripting
	class_viewinfog 1	
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_l1 none dummy 0 {} {
	class_light {0 0 0} 4 {0.49 0.65 0.85} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_l2 none dummy 0 {} {
	class_light {0 0 0} 2 {0.49 0.65 0.85} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_m none dummy 0 {} {
	class_light {0 0 0} 6 {0.65 0.51 0.3} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Lava_LightSrc_n none dummy 0 {} {
	class_light {0 0 0} 6 {0.85 0.56 0.5} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_o none dummy 0 {} {
	class_light {0 0 0} 6 {0.6 0.37 0.77} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_o1 none dummy 0 {} {
	class_light {0 0 0} 4 {0.6 0.37 0.77} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



def_class Urw_LightSrc_o2 none dummy 0 {} {
	class_light {0 0 0} 2 {0.6 0.37 0.77} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Lava_LightSrc_p none dummy 0 {} {
	class_light {0 0 0} 6 {0.95 0.39 0.13} -1
	class_disablescripting
	class_viewinfog 1	
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Lava_LightSrc_p1 none dummy 0 {} {
	class_light {0 0 0} 4 {0.95 0.39 0.13} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Lava_LightSrc_p2 none dummy 0 {} {
	class_light {0 0 0} 2 {0.95 0.39 0.13} -1
	class_disablescripting
	class_viewinfog 1
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_q none dummy 0 {} {
	class_light {0 0 0} 6 {0.08 0.34 0.82} -1
	class_disablescripting
	class_viewinfog 1	
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_q1 none dummy 0 {} {
	class_light {0 0 0} 4 {0.08 0.34 0.82} -1
	class_disablescripting
	class_viewinfog 1	
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}


def_class Urw_LightSrc_q2 none dummy 0 {} {
	class_light {0 0 0} 2 {0.08 0.34 0.82} -1
	class_disablescripting
	class_viewinfog 1	
	if {[get_mapedit]} {class_defaultanim lscr_a.standard}
}



