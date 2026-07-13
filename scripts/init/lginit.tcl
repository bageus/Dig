if { [get_mapedit] } { return }

call data/scripts/init/lgtools.tcl

lg_addfilterclass {	Zwerg Wuker Troll Pilz Kohle Kristallerz Golderz Eisenerz Schatztonne }

LG_set_templategroup <Urwald>	{

	Std {
		Gng { 'tcl/urw_gng_*' }
		Gsg { 'tcl/urw_gsg_*' }
		Hol { 'tcl/urw_hol_*' }
	}
}

LG_set_templategroup <Metall>	{

	Std {
		Gng { 'tcl/swf_gng_*' }
		Gsg { 'tcl/swf_gsg_*' }
		Hol { 'tcl/swf_hol_*' }
	}
}

LG_set_templategroup <Kristall>	{

	Std {
		Gng { 'tcl/kris_gng_*' }
		Gsg { 'tcl/kris_gsg_*' }
		Hol { 'tcl/kris_hol_*' }
	}
}

LG_set_templategroup <Lava>	{

	Std {
		Gng { 'tcl/lava_gng_*' }
		Gsg { 'tcl/lava_gsg_*' }
		Hol { 'tcl/lava_hol_*' }
	}
}


	#<tmp> <gc> <lc> <tu>
LG_set_templateprops {
	{urw_gng_050_a 	1	1	-	}
	{urw_gng_051_a 	1	1	-	}
	{urw_gng_052_a 	1	1	-	}
	{urw_gng_054_a 	1	1	-	}
}


# lg_set_templateprops	001.tcl -gamecount 2 -levelcount 1 -tunnel 0.5
# lg_set_templateprops	002.tcl -levelcount 23 -tunnel 0.5

