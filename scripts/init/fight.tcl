// fight.tcl

fight_map_obj2db Zwerg 			mann
fight_map_obj2db Geisel			mann
fight_map_obj2db Holzpuppe 		oberwelt
fight_map_obj2db Trainingspuppe oberwelt
fight_map_obj2db Krake_ 		krake
fight_map_obj2db Drachenbaby	drache01
fight_map_obj2db Riesenlaufrad	kristall
fight_map_obj2db Alienpflanze	flusslandschaft
fight_map_obj2db Fresspflanze	flusslandschaft
fight_map_obj2db Schwefelwuker	wuker
fight_map_obj2db Kristallbrut	brut
fight_map_obj2db Lavabrut		brut
fight_map_obj2db Fenris			fenrir
fight_map_obj2db FenrisFuss		kampf
fight_map_obj2db ElfenFluegelA	kampf
fight_map_obj2db ElfenFluegelB	kampf
fight_map_obj2db ElfenFluegelC	kampf
fight_map_obj2db ElfenFluegelD	kampf

// fight_action <name> <usr> <pose> <typ> <vdir> <hdir> <coll> <anm> <atk> [<minexp>] @fight@ define fight action
//  name - name of action
//  usr - user class id or -1 for gnome
//  typ - action type: beat,stab,throw,ward,dodge,hit,idle
//  vdir - vert. direction: u = up,s = straight,d = down,w = wall
//  hdir - hor.  direction: f = front, l = left, r = right, b = back
//  coll - collision (bool)
//  anm - anim id or name: [db_findanim ...] or 'kungfu_faust_oben'
//  atk - attack/defense value: 0..1 or more with bonus
//  pos - pose: normal,kungfu,sword,twohand,shield,ballist
//	minexp - minimal exp (only for attack!)

//				<name>	<usr>	<pose>	<typ>	<vdir>	<hdir>	<coll>	<anm>						<atk>	[<minexp>]
//Idle:
fight_action	ZIdl1	Zwerg	kungfu	idle	n		n		0		kungfu_standanim			0
fight_action	ZIdl2	Zwerg	sword	idle	n		n		0		schwert_standanim			0
fight_action	ZIdl3	Zwerg	twohand	idle	n		n		0		zweihand_standanim			0
fight_action	ZIdl4	Zwerg	kungfu	idle	w		n		0		kletterstand_anim			0

//Defense:

fight_action	ZBlo1	Zwerg	kungfu	ward	u		f		0		kungfu_oben_blo				0.1
fight_action	ZBlo2	Zwerg	kungfu	ward	s		f		0		kungfu_mitte_blo			0.1
fight_action	ZBlo3	Zwerg	kungfu	ward	d		f		0		kungfu_unten_blo			0.1

fight_action	ZAusw0	Zwerg	kungfu	dodge	usd		flr		1		kungfu_taenzel_a			0.0

fight_action	ZAusw1	Zwerg	kungfu	dodge	u		flrb	0		kungfu_ausw_duck			0.0		0.04
fight_action	ZAusw3	Zwerg	kungfu	dodge	usd		flr		1		kungfu_ausw_zurueck 		0.0		0.01
fight_action	ZAusw4	Zwerg	kungfu	dodge	usd		lf		1		kungfu_ausw_seite 			0.0		0.02
fight_action	ZAusw5	Zwerg	kungfu	dodge	usd		lf		1		kungfu_ausw_seite_b			0.0		0.03
fight_action	ZAusw6	Zwerg	kungfu	dodge	d		flrb	0		kungfu_ausw_jump 			0.0		0.07

fight_action	ZBlo4	Zwerg	sword	ward	u		f		0		schwert_oben_blo			0.21
fight_action	ZBlo5	Zwerg	sword	ward	s		f		0		schwert_mitte_blo			0.21
fight_action	ZBlo6	Zwerg	sword	ward	d		f		0		schwert_unten_blo			0.21
fight_action	ZAusw7	Zwerg	sword	dodge	usd		flr		1		schwert_ausw_zurueck		0.0		0.01
fight_action	ZAusw8	Zwerg	sword	dodge	u		flrb	1		schwert_ausw_duck			0.0		0.05
fight_action	ZAusw9	Zwerg	sword	dodge	d		flrb 	0		schwert_ausw_jump 			0.0		0.1

fight_action	ZBlo7	Zwerg	twohand	ward	u	 	f		0		zweihand_oben_blo			0.23
fight_action	ZBlo8	Zwerg	twohand	ward	s	 	f		0		zweihand_mitte_blo			0.23
fight_action	ZBlo9	Zwerg	twohand	ward	d	 	f		0		zweihand_unten_blo			0.23
fight_action	ZBlo10	Zwerg	twohand	ward	d	 	fr		0		zweihand_oben_runterblock	0.23
fight_action	ZAusw10	Zwerg	twohand	dodge	d	 	flrb	0		zweihand_ausw_jump			0.0		0.1
fight_action	ZAusw11	Zwerg	twohand	dodge	d	 	flb		1		zweihand_ausw_seite_b		0.0		0.05

fight_action	ZBlo11  Zwerg	shield	ward	u 		f		0		schild_oben_blo				0.23
fight_action	ZBlo12  Zwerg	shield	ward	s 		f		0		schild_mitte_blo			0.23
fight_action	ZBlo13  Zwerg	shield	ward	d 		f		0		schild_unten_blo			0.23
fight_action	ZAusw12 Zwerg	shield	dodge	u 		flrb	0		schwert_ausw_duck			0.0		0.05
fight_action	ZBlo14  Zwerg	shield	ward	s 		f		0		stand_schild_blo			0.23

//Attack:

fight_action	ZBeat1	Zwerg	kungfu	beat	u		f		0		kungfu_faust_oben			0.5
fight_action	ZBeat2	Zwerg	kungfu	beat	s		f		0		kungfu_faust_mitte			0.5
fight_action	ZBeat3	Zwerg	kungfu	beat	u		f		0		kungfu_hand_oben_gerade		0.6		0.03
fight_action	ZBeat4	Zwerg	kungfu	beat	s		f		0		kungfu_hand_mitte_gerade	0.6		0.03
fight_action	ZBeat5	Zwerg	kungfu	beat	u		f		0		kungfu_fuss_oben_dreh		1.0		0.2
fight_action	ZBeat6	Zwerg	kungfu	beat	s		f		0		kungfu_fuss_mitte_gerade	0.7		0.1
fight_action	ZBeat7	Zwerg	kungfu	beat	d		f		0		kungfu_fuss_unten_gerade	0.67	0.05
fight_action	ZBeat8	Zwerg	kungfu	beat	s		r		0		kungfu_treten_seite			0.65	0.06
fight_action	ZBeat9	Zwerg	kungfu	beat	s		l		0		kungfu_schlagen_seite		0.48
fight_action	ZBeat9	Zwerg	kungfu	beat	d		f		0		kungfu_sprung_unten			0.7		0.13
fight_action	ZBeat9	Zwerg	kungfu	beat	d		f		0		kungfu_sprung_mitte			0.72	0.15
fight_action	ZBeat9	Zwerg	kungfu	beat	d		f		0		kungfu_sprung_oben			0.74	0.19
fight_action	ZBeat17	Zwerg	kungfu	beat	u		f		0		kungfu_meisterkick_oben		3.0		0.39

fight_action	ZBeat16	Zwerg	kungfu	beat	w		fr		0		kletter_schlagen_a			0.25
fight_action	ZBeat16	Zwerg	kungfu	beat	w		lb		0		kletter_treten_a			0.31
fight_action	ZBeat16	Zwerg	sword	beat	w		fr		0		kletter_schlagen_a			0.25
fight_action	ZBeat16	Zwerg	sword	beat	w		lb		0		kletter_treten_a			0.31
fight_action	ZBeat16	Zwerg	twohand	beat	w		fr		0		kletter_schlagen_a			0.25
fight_action	ZBeat16	Zwerg	twohand	beat	w		lb		0		kletter_treten_a			0.31



fight_action	ZBeat10	Zwerg	sword	beat	u		f		0		schwert_oben_hieb			0.32
fight_action	ZStab1	Zwerg	sword	stab	u 		f		0		schwert_oben_stech			0.32
fight_action	ZBeat11	Zwerg	sword	beat	s 		f		0		schwert_mitte_hieb			0.32
fight_action	ZStab2	Zwerg	sword	stab	s 		f		0		schwert_mitte_stech			0.32
fight_action	ZBeat12	Zwerg	sword	beat	d 		f		0		schwert_unten_hieb			0.32
fight_action	ZStab3	Zwerg	sword	stab	d 		f		0		schwert_unten_stech			0.32
fight_action	ZStab3	Zwerg	sword	stab	s 		f		0		schwert_rauf_hieb			0.37	0.19
fight_action	ZStab3	Zwerg	sword	stab	s 		f		0		schwert_runter_hieb			0.37	0.19
fight_action	ZStab3	Zwerg	sword	stab	u 		f		0		schwert_oben_meisterhieb	0.9		0.39

fight_action	ZBeat13	Zwerg	twohand	beat	u		f 		0		zweihand_oben_hieb			0.36
fight_action	ZBeat14	Zwerg	twohand	beat	s 		f		0		zweihand_mitte_hieb			0.36
fight_action	ZBeat15	Zwerg	twohand	beat	d 		f		0		zweihand_unten_hieb			0.36
fight_action	ZBeat15	Zwerg	twohand	beat	s 		f		0		zweihand_rauf_hieb			0.41	0.19
fight_action	ZBeat15	Zwerg	twohand	beat	s 		f		0		zweihand_runter_hieb		0.41	0.19
fight_action	ZBeat15	Zwerg	twohand	beat	u 		f		0		zweihand_oben_drehhieb		0.9		0.39

fight_action    ZBow1	Zwerg	ballist throw 	s		f 		0		schiessen_katschi			0.3
fight_action    ZBow2	Zwerg	ballist throw 	s		f 		0		schiessen_bogen_loop		0.3
fight_action    ZBow3	Zwerg	ballist throw 	s		f 		0		schiessen_flinte_loop		0.3
fight_action    ZBow4	Zwerg	ballist throw 	s		f 		0		schiessen_ak_loop			0.3
fight_action    ZBow4	Zwerg	ballist throw 	s		f 		0		schiessen_mp5_loop			0.3
fight_action    ZBow4	Zwerg	ballist throw 	s		f 		0		schiessen_m4_loop			0.3
fight_action    ZBow4	Zwerg	ballist throw 	s		f 		0		schiessen_para_loop			0.3
fight_action    ZBow4	Zwerg	ballist throw 	s		f 		0		schiessen_m3_super_90_loop	0.3
fight_action    ZBow4	Zwerg	ballist throw 	s		f 		0		schiessen_duals_loop		0.3
fight_action    ZBow4	Zwerg	ballist throw 	s		f 		0		schiessen_awp_loop			0.3
fight_action    ZBow4	Zwerg	ballist throw 	s		f 		0		schiessen_deagle_loop		0.3

//GotHit:
fight_action	ZHit25	Zwerg	kungfu	hit		w		flrb	0		kletterstand_anim			0
fight_action	ZHit25	Zwerg	kungfu	hit		w		flrb	0		kletterstand_anim			-1

fight_action	ZKHit1	Zwerg	kungfu	hit		u		flr		0		kungfu_oben_get_leicht		0     																																																																								    ð
fight_action	ZKHit2	Zwerg	kungfu	hit		u		f		1		kungfu_oben_get_mittel		0     																																																																								    ð
fight_action	ZKHit3	Zwerg	kungfu	hit		u		f		1		kungfu_oben_get_schwer		0   																																																																								    ð
fight_action	ZKHit4	Zwerg	kungfu	hit		u		flr		0		kungfu_oben_get_tot			-1      																																																																								    ð
fight_action	ZKHit5	Zwerg	kungfu	hit 	s		flr		0		kungfu_mitte_get_leicht		0  																																																																							    ð
fight_action	ZKHit6	Zwerg	kungfu	hit 	s		f		0		kungfu_mitte_get_mittel		0  																																																																							    ð
fight_action	ZKHit7	Zwerg	kungfu	hit 	s		f		1		kungfu_mitte_get_schwer		0  																																																																							    ð
fight_action	ZKHit8	Zwerg	kungfu	hit 	s		flr		0		kungfu_mitte_get_tot		-1  																																																																								    ð
fight_action	ZKHit9	Zwerg	kungfu	hit		d		flr		0		kungfu_unten_get_leicht		0     																																																																								    ð
fight_action	ZKHit10	Zwerg	kungfu	hit		d		f		0		kungfu_unten_get_mittel		0     																																																																								    ð
fight_action	ZKHit11	Zwerg	kungfu	hit		d		f		1		kungfu_unten_get_schwer		0   																																																																								    ð
fight_action	ZKHit12	Zwerg	kungfu	hit		d		f		0		kungfu_unten_get_tot		-1      																																																																								    ð
fight_action	ZKHit13	Zwerg	kungfu  hit		usd		b		0		kungfu_hinten_get_leicht	0  																																																																							    ð
fight_action	ZKHit14	Zwerg	kungfu  hit		usd		b		1		kungfu_hinten_get_mittel	0  																																																																							    ð
fight_action	ZKHit15	Zwerg	kungfu  hit		usd		b		1		kungfu_hinten_get_schwer	0  																																																																							    ð
fight_action	ZKHit16	Zwerg	kungfu  hit		usd		b		0		kungfu_hinten_get_tot		-1  																																																																								    ð

fight_action	ZSHit1	Zwerg	sword	hit		u		flr		0		schwert_oben_get_leicht		0
fight_action	ZSHit2	Zwerg	sword	hit		u 		f		1		schwert_oben_get_mittel		0
fight_action	ZSHit3	Zwerg	sword	hit		u 		f		1		schwert_oben_get_schwer		0
fight_action	ZSHit4	Zwerg	sword	hit		u 		flr		0		schwert_oben_get_tot		-1
fight_action	ZSHit5	Zwerg	sword	hit		s 		flr		0		schwert_mitte_get_leicht	0
fight_action	ZSHit6	Zwerg	sword	hit	 	s 		f		1		schwert_mitte_get_mittel	0
fight_action	ZSHit7	Zwerg	sword	hit	 	s 		f		1		schwert_mitte_get_schwer	0
fight_action	ZSHit8	Zwerg	sword	hit 	s 		flr		0		schwert_mitte_get_tot		-1
fight_action	ZSHit9	Zwerg	sword	hit		d 		flr		0		schwert_unten_get_leicht	0
fight_action	ZSHit10	Zwerg	sword	hit		d 		f		1		schwert_unten_get_mittel	0
fight_action	ZSHit11	Zwerg	sword	hit		d 		f		2		schwert_unten_get_schwer	0
fight_action	ZSHit12	Zwerg	sword	hit		d 		f		0		schwert_unten_get_tot		-1
fight_action	ZSHit13	Zwerg	sword	hit		usd		b		0		schwert_hinten_get_leicht	0
fight_action	ZSHit14	Zwerg	sword	hit		usd		b		1		schwert_hinten_get_mittel	0
fight_action	ZSHit15	Zwerg	sword	hit		usd		b		1		schwert_hinten_get_schwer	0
fight_action	ZSHit16	Zwerg	sword	hit		usd		b		0		schwert_hinten_get_tot		-1

fight_action	ZZHit1	Zwerg	twohand	hit		u 		flr		0	    zweihand_oben_get_leicht	0
fight_action	ZZHit2	Zwerg	twohand	hit		u 		f		1		zweihand_oben_get_mittel	0
fight_action	ZZHit3	Zwerg	twohand	hit		u 		f		1		zweihand_oben_get_schwer	0
fight_action	ZZHit4	Zwerg	twohand	hit		u 		flr		0		zweihand_oben_get_tot		-1
fight_action	ZZHit5	Zwerg	twohand	hit 	s 		flr		0		zweihand_mitte_get_leicht	0
fight_action	ZZHit6	Zwerg	twohand	hit 	s 		f		1		zweihand_mitte_get_mittel	0
fight_action	ZZHit7	Zwerg	twohand	hit 	s 		f		1		zweihand_mitte_get_schwer	0
fight_action	ZZHit8	Zwerg	twohand	hit 	s 		flr		0		zweihand_mitte_get_tot		-1
fight_action	ZZHit9	Zwerg	twohand	hit		d 		flr		0		zweihand_unten_get_leicht	0
fight_action	ZZHit10	Zwerg	twohand	hit		d 		f		1		zweihand_unten_get_mittel	0
fight_action	ZZHit11	Zwerg	twohand	hit		d 		f		2		zweihand_unten_get_schwer	0
fight_action	ZZHit12	Zwerg	twohand	hit		d 		f		0		zweihand_unten_get_tot		-1
fight_action	ZZHit13	Zwerg	twohand	hit		usd		b		0		zweihand_hinten_get_leicht 	0
fight_action	ZZHit14	Zwerg	twohand	hit		usd		b		1		zweihand_hinten_get_mittel 	0
fight_action	ZZHit15	Zwerg	twohand	hit		usd		b		1		zweihand_hinten_get_schwer 	0
fight_action	ZZHit16	Zwerg	twohand	hit		usd		b		0		zweihand_hinten_get_tot		-1

fight_action	ZKHit17	Zwerg	shield	hit		u 		flr		0	    stand_vorne_get_leicht		0
fight_action	ZKHit18	Zwerg	shield	hit		u 		f		1		stand_vorne_get_mittel		0
fight_action	ZKHit19	Zwerg	shield	hit		u 		f		1		stand_vorne_get_schwer		0
fight_action	ZKHit20	Zwerg	shield	hit		u 		flr		0		stand_vorne_get_tot			-1
fight_action	ZKHit21	Zwerg	shield	hit 	usd 	b		0	   	stand_hinten_get_leicht		0
fight_action	ZKHit22	Zwerg	shield	hit 	usd 	b		1		stand_hinten_get_mittel		0
fight_action	ZKHit23	Zwerg	shield	hit 	usd		b		1		stand_hinten_get_schwer		0
fight_action	ZKHit24	Zwerg	shield	hit 	usd 	b		0		stand_hinten_get_tot		-1

// Geisel:
fight_action	GIdl1	Geisel	kungfu	idle	n		n		0		kungfu_standanim			0
fight_action	GHit25	Geisel	kungfu	hit		w		flrb	0		kletterstand_anim			0
fight_action	GHit25	Geisel	kungfu	hit		w		flrb	0		kletterstand_anim			-1

fight_action	GKHit1	Geisel	kungfu	hit		u		flr		0		kungfu_oben_get_leicht		0     																																																																								    ð
fight_action	GKHit2	Geisel	kungfu	hit		u		f		1		kungfu_oben_get_mittel		0     																																																																								    ð
fight_action	GKHit3	Geisel	kungfu	hit		u		f		1		kungfu_oben_get_schwer		0   																																																																								    ð
fight_action	GKHit4	Geisel	kungfu	hit		u		flr		0		kungfu_oben_get_tot			-1      																																																																								    ð
fight_action	GKHit5	Geisel	kungfu	hit 	s		flr		0		kungfu_mitte_get_leicht		0  																																																																							    ð
fight_action	GKHit6	Geisel	kungfu	hit 	s		f		0		kungfu_mitte_get_mittel		0  																																																																							    ð
fight_action	GKHit7	Geisel	kungfu	hit 	s		f		1		kungfu_mitte_get_schwer		0  																																																																							    ð
fight_action	GKHit8	Geisel	kungfu	hit 	s		flr		0		kungfu_mitte_get_tot		-1  																																																																								    ð
fight_action	GKHit9	Geisel	kungfu	hit		d		flr		0		kungfu_unten_get_leicht		0     																																																																								    ð
fight_action	GKHit10	Geisel	kungfu	hit		d		f		0		kungfu_unten_get_mittel		0     																																																																								    ð
fight_action	GKHit11	Geisel	kungfu	hit		d		f		1		kungfu_unten_get_schwer		0   																																																																								    ð
fight_action	GKHit12	Geisel	kungfu	hit		d		f		0		kungfu_unten_get_tot		-1      																																																																								    ð
fight_action	GKHit13	Geisel	kungfu  hit		usd		b		0		kungfu_hinten_get_leicht	0  																																																																							    ð
fight_action	GKHit14	Geisel	kungfu  hit		usd		b		1		kungfu_hinten_get_mittel	0  																																																																							    ð
fight_action	GKHit15	Geisel	kungfu  hit		usd		b		1		kungfu_hinten_get_schwer	0  																																																																							    ð
fight_action	GKHit16	Geisel	kungfu  hit		usd		b		0		kungfu_hinten_get_tot		-1  																																																																								    ð

//Wuker:
//idle
fight_action	WIdl1	Wuker	kungfu	idle	n		n		0		stand_atmen_a				0
//attack
fight_action	WBeat1	Wuker	kungfu	beat	s		f	 	0		beissen_mitte				0.5
fight_action	WBeat2	Wuker	kungfu	beat	u		f	 	0		boxen_oben					0.5
fight_action	WBeat3	Wuker	kungfu	beat	u		f	 	0		treten_oben					0.5
fight_action	WBeat4	Wuker	kungfu	beat	u		f	 	0		schlagen_oben 				0.45

fight_action	WBeat5	Wuker	kungfu	beat	w		fr		0		kletter_schlagen_a			0.4
fight_action	WBeat6	Wuker	kungfu	beat	w		lb		0		kletter_treten_a			0.4

//got hit
fight_action	WHit1	Wuker	kungfu	hit		s		flrb	0		oben_get_leicht				0
fight_action	WHit2	Wuker	kungfu	hit 	u		flrb	0		mitte_get_leicht			0
fight_action	WHit3	Wuker	kungfu	hit 	d		flrb	0		unten_get_leicht			0

fight_action	WHit4	Wuker	kungfu	hit 	s		f	 	1		oben_get_mittel				0
fight_action	WHit5	Wuker	kungfu	hit 	u		f	 	1		mitte_get_mittel			0
fight_action	WHit6	Wuker	kungfu	hit 	d		f	 	1		unten_get_mittel			0

fight_action	WHit7	Wuker	kungfu	hit 	s		f	 	1		oben_get_schwer				0
fight_action	WHit8	Wuker	kungfu	hit 	u		f	 	1		mitte_get_schwer			0
fight_action	WHit9	Wuker	kungfu	hit 	d		f	 	1		unten_get_schwer			0

fight_action	WHit10	Wuker	kungfu	hit 	s		flrb 	0		oben_get_tot				-1
fight_action	WHit11	Wuker	kungfu	hit 	u		flrb 	0		mitte_get_tot				-1
fight_action	WHit12	Wuker	kungfu	hit 	d		flrb 	0		unten_get_tot				-1

fight_action	WHit13	Wuker	kungfu	hit		w		flrb	0		kletter_runterfallen		0
fight_action	WHit14	Wuker	kungfu	hit		w		flrb	0		kletter_runterfallen		-1

//Schwefelwuker:
//idle
set sw "Schwefelwuker"
fight_action	SSWIdl1	$sw		kungfu	idle	n		n		0		stand_atmen_a				0
//attack
fight_action	SWBeat1	$sw		kungfu	beat	s		f	 	0		beissen_mitte				0.7
fight_action	SWBeat2	$sw		kungfu	beat	u		f	 	0		boxen_oben					0.6
fight_action	SWBeat3	$sw		kungfu	beat	u		f	 	0		treten_oben					0.7
fight_action	SWBeat4	$sw		kungfu	beat	u		f	 	0		schlagen_oben 				0.5

fight_action	SWBeat5	$sw		kungfu	beat	w		fr		0		kletter_schlagen_a			0.4
fight_action	SWBeat6	$sw		kungfu	beat	w		lb		0		kletter_treten_a			0.5

//got hit
fight_action	SWHit1	$sw		kungfu	hit		s		flrb	0		oben_get_leicht				0
fight_action	SWHit2	$sw		kungfu	hit 	u		flrb	0		mitte_get_leicht			0
fight_action	SWHit3	$sw		kungfu	hit 	d		flrb	0		unten_get_leicht			0

fight_action	SWHit4	$sw		kungfu	hit 	s		f	 	1		oben_get_mittel				0
fight_action	SWHit5	$sw		kungfu	hit 	u		f	 	1		mitte_get_mittel			0
fight_action	SWHit6	$sw		kungfu	hit 	d		f	 	1		unten_get_mittel			0

fight_action	SWHit7	$sw		kungfu	hit 	s		f	 	1		oben_get_schwer				0
fight_action	SWHit8	$sw		kungfu	hit 	u		f	 	1		mitte_get_schwer			0
fight_action	SWHit9	$sw		kungfu	hit 	d		f	 	1		unten_get_schwer			0

fight_action	SWHit10	$sw		kungfu	hit 	s		flrb 	0		oben_get_tot				-1
fight_action	SWHit11	$sw		kungfu	hit 	u		flrb 	0		mitte_get_tot				-1
fight_action	SWHit12	$sw		kungfu	hit 	d		flrb 	0		unten_get_tot				-1

fight_action	SWHit13	$sw		kungfu	hit		w		flrb	0		kletter_runterfallen		0
fight_action	SWHit14	$sw		kungfu	hit		w		flrb	0		kletter_runterfallen		-1


//Troll:
//idle
fight_action	TIdl1	Troll	kungfu	idle	n   	n		0		stehen_warten_a				0
fight_action	TIdl2	Troll	kungfu	idle	n   	n		0		schwert_warten_a			0
fight_action	TIdl3	Troll	kungfu	idle	n   	n		0		speer_warten_a				0
//attack
fight_action	TBeat1	Troll	kungfu	beat 	u       f		0		stehen_watschen				0.6
fight_action	TBeat2	Troll	kungfu	beat 	s       f		0		stehen_faustschlag			0.55
fight_action	TBeat2	Troll	kungfu	beat 	s       l		0		stehen_faustschlag_links	0.5
fight_action	TBeat2	Troll	kungfu	beat 	s       r		0		stehen_faustschlag_rechts	0.5
fight_action	TBeat3	Troll	kungfu	beat 	d       f		0		stehen_fusstritt			0.6

fight_action	TBeat4	Troll	sword	beat 	u       f		0		schwert_oben				0.3
fight_action	TBeat5	Troll	sword	beat 	s       f		0		schwert_mitte				0.3
fight_action	TBeat6	Troll	sword	beat 	d       f		0		schwert_unten				0.3

fight_action	TBeat7	Troll	twohand	beat 	u       f		0		speer_oben					0.2
fight_action	TBeat8	Troll	twohand	beat 	s       f		0		speer_mitte_a				0.2
fight_action	TBeat9	Troll	twohand	stab 	s       f		0		speer_mitte_b				0.2
fight_action	TBeat10	Troll	twohand	stab 	d       f		0		speer_mitte_c				0.2

fight_action	TBeat11	Troll	kungfu	beat	w		f		0		haengen_schlag_oben			0.3
fight_action	TBeat12	Troll	kungfu	beat	w		l		0		haengen_schlag_links		0.3
fight_action	TBeat13	Troll	kungfu	beat	w		r		0		haengen_schlag_rechts		0.3
fight_action	TBeat14	Troll	kungfu	beat	w		b		0		haengen_schlag_unten		0.4

fight_action	TBeat11	Troll	sword	beat	w		f		0		haengen_schlag_oben			0.3
fight_action	TBeat12	Troll	sword	beat	w		l		0		haengen_schlag_links		0.3
fight_action	TBeat13	Troll	sword	beat	w		r		0		haengen_schlag_rechts		0.3
fight_action	TBeat14	Troll	sword	beat	w		b		0		haengen_schlag_unten		0.4

fight_action	TBeat11	Troll	twohand	beat	w		f		0		haengen_schlag_oben			0.3
fight_action	TBeat12	Troll	twohand	beat	w		l		0		haengen_schlag_links		0.3
fight_action	TBeat13	Troll	twohand	beat	w		r		0		haengen_schlag_rechts		0.3
fight_action	TBeat14	Troll	twohand	beat	w		b		0		haengen_schlag_unten		0.4

//got hit
fight_action	THit1	Troll	kungfu	hit 	s		flr		0		vorne_mitte_get_leicht		0
fight_action	THit4	Troll	kungfu	hit 	s		f		1		vorne_mitte_get_mittel		0
fight_action	THit7	Troll	kungfu	hit 	s		f		1		vorne_mitte_get_schwer		0
fight_action	THit7	Troll	kungfu	hit 	s		flr		0		vorne_mitte_get_tot 		-1

fight_action	THit1	Troll	kungfu	hit 	u		flr		0		vorne_oben_get_leicht		0
fight_action	THit4	Troll	kungfu	hit 	u		f		1		vorne_oben_get_mittel		0
fight_action	THit7	Troll	kungfu	hit 	u		f		1		vorne_oben_get_schwer		0
fight_action	THit7	Troll	kungfu	hit 	u		flr		0		vorne_oben_get_tot 			-1

fight_action	THit1	Troll	kungfu	hit 	d		flr		0		vorne_unten_get_leicht		0
fight_action	THit4	Troll	kungfu	hit 	d		f		1		vorne_unten_get_mittel		0
fight_action	THit7	Troll	kungfu	hit 	d		f		1		vorne_unten_get_schwer		0
fight_action	THit7	Troll	kungfu	hit 	d		flr		0		vorne_unten_get_tot 		-1


fight_action	THit10	Troll	kungfu	hit		usd		b		0		stehen_hinten_get_leicht 	0
fight_action	THit13	Troll	kungfu	hit		usd		b		1		stehen_hinten_get_mittel 	0
fight_action	THit16	Troll	kungfu	hit		usd		b		1		stehen_hinten_get_schwer 	0
fight_action	THit21	Troll	kungfu	hit		usd		b		0		stehen_hinten_get_tot 		-1

fight_action	THit22	Troll	sword	hit		usd		b		0		schwert_hinten_get_leicht 	0
fight_action	THit23	Troll	sword	hit		usd		b		1		schwert_hinten_get_mittel 	0
fight_action	THit24	Troll	sword	hit		usd		b		1		schwert_hinten_get_schwer 	0
fight_action	THit25	Troll	sword	hit		usd		b		0		schwert_hinten_get_tot  	-1
fight_action	THit26	Troll	sword	hit		s		flr		0		schwert_mitte_get_leicht 	0
fight_action	THit27	Troll	sword	hit		s		f		1		schwert_mitte_get_mittel 	0
fight_action	THit28	Troll	sword	hit		s		f		1		schwert_mitte_get_schwer 	0
fight_action	THit29	Troll	sword	hit		s		flr		0		schwert_mitte_get_tot   	-1
fight_action	THit30	Troll	sword	hit		u		flr		0		schwert_oben_get_leicht  	0
fight_action	THit31	Troll	sword	hit		u		f		1		schwert_oben_get_mittel		0
fight_action	THit32	Troll	sword	hit		u		f		1		schwert_oben_get_schwer		0
fight_action	THit33	Troll	sword	hit		u		flr		0		schwert_oben_get_tot   		-1
fight_action	THit34	Troll	sword	hit		d		flr		0		schwert_unten_get_leicht 	0
fight_action	THit35	Troll	sword	hit		d		f		1		schwert_unten_get_mittel 	0
fight_action	THit36	Troll	sword	hit		d		f		1		schwert_unten_get_schwer 	0
fight_action	THit37	Troll	sword	hit		d		flr		0		schwert_unten_get_tot   	-1

fight_action	THit38	Troll	twohand	hit		usd     b    	0		speer_hinten_get_leicht 	0
fight_action	THit39	Troll	twohand	hit		usd     b    	1		speer_hinten_get_mittel 	0
fight_action	THit40	Troll	twohand	hit		usd     b    	1		speer_hinten_get_schwer 	0
fight_action	THit41	Troll	twohand	hit		usd     b    	0		speer_hinten_get_tot  		-1
fight_action	THit42	Troll	twohand	hit		s       flr    	0		speer_mitte_get_leicht 		0
fight_action	THit43	Troll	twohand	hit		s       f    	1		speer_mitte_get_mittel 		0
fight_action	THit44	Troll	twohand	hit		s       f    	1		speer_mitte_get_schwer 		0
fight_action	THit45	Troll	twohand	hit		s       flr    	0		speer_mitte_get_tot   		-1
fight_action	THit46	Troll	twohand	hit		u       flr    	0		speer_oben_get_leicht 	 	0
fight_action	THit47	Troll	twohand	hit		u       f    	1		speer_oben_get_mittel 	 	0
fight_action	THit48	Troll	twohand	hit		u       f    	1		speer_oben_get_schwer 	 	0
fight_action	THit49	Troll	twohand	hit		u       flr    	0		speer_oben_get_tot    		-1
fight_action	THit50	Troll	twohand	hit		d       flr    	0		speer_unten_get_leicht	 	0
fight_action	THit51	Troll	twohand	hit		d       f    	1		speer_unten_get_mittel 		0
fight_action	THit52	Troll	twohand	hit		d       f    	1		speer_unten_get_schwer 		0
fight_action	THit53 	Troll	twohand	hit		d       flr    	0		speer_unten_get_tot  		-1

fight_action	THit54	Troll	kungfu	hit		w		flrb	0		klettern_warten				0
fight_action	THit55	Troll	kungfu	hit		w		flrb	0		klettern_warten				-1


fight_action	TAusw1	Troll	sword	dodge 	d       f	 	0		schwert_ausw_unten   		0
fight_action	TAusw2	Troll	sword	dodge 	d     	flrb  	0		schwert_ausw_sprung  		0

fight_action	TBlo1	Troll	sword	ward 	d       f 	  	0		schwert_blo_unten  			0.25
fight_action	TBlo2	Troll	sword	ward 	s       f 	  	0		schwert_blo_mitte  			0.25
fight_action	TBlo3	Troll	sword	ward 	u       f 	  	0		schwert_blo_oben   			0.25

fight_action	TAusw4	Troll	twohand	dodge 	d       f	  	0		speer_ausw_unten  			0
fight_action	TAusw5	Troll	twohand	dodge 	d       flrb  	0		speer_ausw_sprung 			0

fight_action	TBlo4	Troll	twohand	ward 	d       f 	  	0		speer_blo_unten  			0.3
fight_action	TBlo5	Troll	twohand	ward 	s       f 	  	0		speer_blo_mitte  			0.3
fight_action	TBlo6	Troll	twohand	ward 	u       f 	  	0		speer_blo_oben   			0.3

fight_action	PAusw1	Holzpuppe kungfu dodge 	usd     flrb 	0		obw_puppe_a.anim			0
fight_action	PBeat1	Holzpuppe kungfu beat 	u     	flrb 	0		obw_puppe_a.anim			0.02
fight_action	PAusw2	Trainingspuppe kungfu dodge usd flrb 	0		obw_puppe_b.anim			0
fight_action	PBeat2	Trainingspuppe kungfu beat u 	flrb 	0		obw_puppe_b.anim			0.04

fight_action	HRIdle1	Riesenlaufrad  kungfu idle	usd flrb 	0		kris_riesenlaufrad.drehen	0
fight_action	HRHit1	Riesenlaufrad  kungfu hit	usd flrb 	0		kris_riesenlaufrad.drehen	0
fight_action	HRHit2	Riesenlaufrad  kungfu hit	usd flrb 	0		kris_riesenlaufrad.zerstoert -1

//Drache
fight_action	DIdl1	Drache	kungfu	idle	n		n		0		sitzen_warten_a				0

fight_action	DHit1	Drache 	kungfu 	hit 	usd		flr 	0		sitzen_get_vorne_leicht		0
fight_action	DHit2	Drache 	kungfu 	hit 	usd 	flr 	0		sitzen_get_vorne_mittel		0
fight_action	DHit3	Drache 	kungfu 	hit 	usd		b	 	0		sitzen_get_hinten_leicht	0
fight_action	DHit4	Drache 	kungfu 	hit 	usd		b 		0		sitzen_get_hinten_mittel	0
fight_action	DHit5	Drache 	kungfu 	hit 	usd		flrb 	0		sitzen_get_toetlich			-1

fight_action	DBeat1	Drache 	kungfu 	beat 	usd		flrb 	0		sitzen_vorbeugen			1.0

//Drachenbaby
set DrB "Drachenbaby"
fight_action	DBIdl1	$DrB	kungfu	idle	n		n		0		stehen_warten				0

fight_action	DBHit1	$DrB 	kungfu 	hit 	usd		flr 	0		stehen_get_vorne_leicht		0
fight_action	DBHit1	$DrB 	kungfu 	hit 	usd		flr 	0		stehen_get_vorne_mittel		0
fight_action	DBHit2	$DrB 	kungfu 	hit 	usd 	flr 	0		stehen_get_vorne_schwer		0
fight_action	DBHit3	$DrB 	kungfu 	hit 	usd		b	 	0		stehen_get_hinten_leicht	0
fight_action	DBHit4	$DrB 	kungfu 	hit 	usd		b 		0		stehen_get_hinten_mittel	0
fight_action	DBHit4	$DrB 	kungfu 	hit 	usd		b 		0		stehen_get_hinten_schwer	0
fight_action	DBHit5	$DrB 	kungfu 	hit 	usd		flrb 	0		stehen_get_vorne_toetlich	-1

fight_action	DBHit1	$DrB 	kungfu 	hit 	w		flr 	0		stehen_get_vorne_leicht		0
fight_action	DBHit1	$DrB 	kungfu 	hit 	w		flr 	0		stehen_get_vorne_mittel		0
fight_action	DBHit2	$DrB 	kungfu 	hit 	w	 	flr 	0		stehen_get_vorne_schwer		0

fight_action	DBBeat1	$DrB 	kungfu 	beat 	usd		flrb 	0		stehen_beissen				1.0

unset DrB

//Krake
fight_action	KBeat1	Krake_ 	kungfu 	beat 	usd		flrb 	0		krake.standard				1.0

//Riesenhamster
set RH "Riesenhamster"
fight_action	RHIdl1	$RH 	kungfu 	idle 	usd		n	 	0		standanim					0

fight_action	RHHit1	$RH 	kungfu 	hit 	usd		flrb 	0		get_vorn_leicht				0
fight_action	RHHit2	$RH 	kungfu 	hit 	usd		flrb 	0		get_vorn_leicht				0
fight_action	RHHit3	$RH 	kungfu 	hit 	usd		f		1		get_vorn_schwer				0
fight_action	RHHit4	$RH 	kungfu 	hit 	usd		flrb 	0		sterben						-1

fight_action	RHBeat1	$RH 	kungfu 	beat 	usd		flr	 	0		schlagen					2.5
unset RH

//Fenris
fight_action	FIdl1	Fenris 	kungfu 	idle 	usd		n	 	0		kampf_standanim				0

fight_action	FBeat1	Fenris 	kungfu 	beat 	usd		fr	 	0		kampf_schlag_a_start		4.0

fight_action	FHit1	Fenris 	kungfu 	hit 	usd		flrb 	0		kampf_standanim				0
fight_action	FHit2	Fenris 	kungfu 	hit 	usd		flrb 	0		kampf_zu_knie				-1

//FenrisFuss
fight_action	FFIdl1	FenrisFuss	kungfu 	idle 	usd	n	 	0		fenris_dummy.standard		0
fight_action	FFHit1	FenrisFuss	kungfu 	hit 	usd	flrb 	0		fenris_dummy.standard		0
fight_action	FFHit2	FenrisFuss 	kungfu 	hit 	usd	flrb 	0		fenris_dummy.standard		-1

//ElfenFluegel (für die Riesenelfe)

fight_action	FFIdl1	ElfenFluegelA 	kungfu 	idle 	usdw	n	 	0	riesenelfe_dummy2_a.standard		0
fight_action	FFHit1	ElfenFluegelA	kungfu 	hit 	usdw	flrb 	0	riesenelfe_dummy2_a.standard		0
fight_action	FFHit2	ElfenFluegelA 	kungfu 	hit 	usdw	flrb 	0	riesenelfe_dummy2_a.standard		-1

fight_action	FFIdl1	ElfenFluegelB 	kungfu 	idle 	usdw	n	 	0	riesenelfe_dummy2_b.standard		0
fight_action	FFHit1	ElfenFluegelB	kungfu 	hit 	usdw	flrb 	0	riesenelfe_dummy2_b.standard		0
fight_action	FFHit2	ElfenFluegelB 	kungfu 	hit 	usdw	flrb 	0	riesenelfe_dummy2_b.standard		-1

fight_action	FFIdl1	ElfenFluegelC 	kungfu 	idle 	usdw	n	 	0	riesenelfe_dummy2_c.standard		0
fight_action	FFHit1	ElfenFluegelC	kungfu 	hit 	usdw	flrb 	0	riesenelfe_dummy2_c.standard		0
fight_action	FFHit2	ElfenFluegelC 	kungfu 	hit 	usdw	flrb 	0	riesenelfe_dummy2_c.standard		-1

fight_action	FFIdl1	ElfenFluegelD 	kungfu 	idle 	usdw	n	 	0	riesenelfe_dummy2_d.standard		0
fight_action	FFHit1	ElfenFluegelD	kungfu 	hit 	usdw	flrb 	0	riesenelfe_dummy2_d.standard		0
fight_action	FFHit2	ElfenFluegelD 	kungfu 	hit 	usdw	flrb 	0	riesenelfe_dummy2_d.standard		-1


//Spinne
fight_action	SIdl1	Spinne	kungfu	idle	n		n		0		drohen						0

fight_action	SHit1	Spinne 	kungfu 	hit 	wusd	flrb 	0		drohen_get_vorne_leicht		0
fight_action	SHit2	Spinne 	kungfu 	hit 	usd		flrb 	0		drohen_get_vorne_mittel		0
fight_action	SHit3	Spinne 	kungfu 	hit 	usd		flrb 	0		drohen_get_vorne_schwer		0
fight_action	SHit4	Spinne 	kungfu 	hit 	usd		flrb 	0		drohen_get_tot_a			-1

fight_action	SBeat1	Spinne 	kungfu 	beat 	usd		f 		0		drohen_beissen				0.9
fight_action	SBeat1	Spinne 	kungfu 	beat 	w		f 		0		drohen_beissen				0.8

//Alienpflanze
fight_action	APIdl1	Alienpflanze kungfu	idle n		n		0		alienpflanze.auf_loop		0

fight_action	APHit1	Alienpflanze kungfu hit usd		flrb 	0		alienpflanze.auf_loop		0
fight_action	APHit2	Alienpflanze kungfu hit usd		flrb 	0		alienpflanze.auf_loop		0
fight_action	APHit3	Alienpflanze kungfu hit usd		flrb 	0		alienpflanze.auf_loop		0
fight_action	APHit4	Alienpflanze kungfu hit usd		flrb 	0		alienpflanze.auf_loop		-1

//Fresspflanze
fight_action	APIdl1	Fresspflanze kungfu	idle n		n		0		fresspflanze.auf_loop		0

fight_action	FPHit1	Fresspflanze kungfu hit usd		flrb 	0		fresspflanze.auf_loop		0
fight_action	FPHit2	Fresspflanze kungfu hit usd		flrb 	0		fresspflanze.auf_loop		0
fight_action	FPHit3	Fresspflanze kungfu hit usd		flrb 	0		fresspflanze.auf_loop		0
fight_action	FPHit4	Fresspflanze kungfu hit usd		flrb 	0		fresspflanze.auf_loop		-1

//Kristallbrut
fight_action	KBIdl1	Kristallbrut kungfu	idle  n		n		0		brut.standard				0

fight_action	KBHit1	Kristallbrut kungfu hit wusd	flr 	0		brut.vorne_get_leicht		0
fight_action	KBHit2	Kristallbrut kungfu hit usd		flr 	0		brut.vorne_get_mittel		0
fight_action	KBHit3	Kristallbrut kungfu hit usd		flr 	0		brut.vorne_get_schwer		0
fight_action	KBHit4	Kristallbrut kungfu hit usd		flr 	0		brut.vorne_get_tot			-1
fight_action	KBHit5	Kristallbrut kungfu hit usd		b 		0		brut.hinten_get_leicht		0
fight_action	KBHit6	Kristallbrut kungfu hit usd		b 		0		brut.hinten_get_mittel		0
fight_action	KBHit7	Kristallbrut kungfu hit usd		b 		0		brut.hinten_get_schwer		0
fight_action	KBHit8	Kristallbrut kungfu hit usd		b	 	0		brut.hinten_get_tot			-1

fight_action	KBBeat1	Kristallbrut kungfu beat usd	f 		0		brut.beissen_a				2.5
fight_action	KBBeat1	Kristallbrut kungfu beat w		f 		0		brut.beissen_a				1.5
fight_action	KBBeat2	Kristallbrut kungfu beat usd	f 		0		brut.hornstoss				2.0		0.001

//Lavabrut
fight_action	LBIdl1	Lavabrut kungfu	idle	n		n		0		brut.standard				0

fight_action	LBHit1	Lavabrut kungfu hit 	wusd	flr 	0		brut.vorne_get_leicht		0
fight_action	LBHit2	Lavabrut kungfu hit 	usd		flr 	0		brut.vorne_get_mittel		0
fight_action	LBHit3	Lavabrut kungfu hit 	usd		flr 	0		brut.vorne_get_schwer		0
fight_action	LBHit4	Lavabrut kungfu hit 	usd		flr 	0		brut.vorne_get_tot			-1
fight_action	LBHit5	Lavabrut kungfu hit 	usd		b 		0		brut.hinten_get_leicht		0
fight_action	LBHit6	Lavabrut kungfu hit 	usd		b 		0		brut.hinten_get_mittel		0
fight_action	LBHit7	Lavabrut kungfu hit 	usd		b 		0		brut.hinten_get_schwer		0
fight_action	LBHit8	Lavabrut kungfu hit		usd		b	 	0		brut.hinten_get_tot			-1

fight_action	LBBeat1	Lavabrut kungfu beat 	usd		f 		0		brut.beissen_a				3.0
fight_action	LBBeat1	Lavabrut kungfu beat 	w		f 		0		brut.beissen_a				2.0
fight_action	LBBeat2	Lavabrut kungfu beat 	usd		f 		0		brut.beissen_b				4.0
fight_action	LBBeat3	Lavabrut kungfu beat 	usd		f 		0		brut.beissen_c				4.0
fight_action	LBBeat4	Lavabrut kungfu beat 	usd		f 		0		brut.fusstritt				3.0		0.001
fight_action	LBBeat5	Lavabrut kungfu beat 	usd		f 		0		brut.sprung_beissen			4.5		0.002

//fight_weapon <name> <atk> <def> <rng> <pos> <atr> <atv> [<minexp>]
//	name - class name of weapon
//  atk - attack  value: 0..1 - weapons
//  def - defense value: 0..1 - shields
//  rng - range
//  pos - pose: Normal,Kungfu,Sword,Twohanded,Ballist
//  atr - Attrib to increase
//  atv - Attrib  Value


fight_weapon none				0.2		0.2		1.0		kungfu		exp_F_Kungfu	2.5		0
//Fernwaffen:<name> 			<atk> 	<def> 	<rng>	<pos>		<atr>			<atv>	<minexp>	(<aniident>)
fight_weapon Steinschleuder 	0.3 	0.0 	6.0		ballist		exp_F_Ballistic	2 		0			katschi  	{ 13 }
fight_weapon PfeilUndBogen 		0.75 	0.0 	9.0		ballist		exp_F_Ballistic	1.5 	0.04		bogen       { 9 }
fight_weapon Buechse 			1.2 	0.0 	15.0	ballist		exp_F_Ballistic	1 		0.1         flinte      { -1 4 }
fight_weapon Trainierbogen		0.01 	0.0 	6.0		ballist		exp_F_Ballistic	1 		0			bogen       { 9 }
fight_weapon AK47				3.0 	0.0 	8.0		ballist		exp_F_Ballistic	1 		0			ak          { -1 1 3 5 7 9 11 }
fight_weapon MP5				1.0 	0.0 	6.0		ballist		exp_F_Ballistic	1 		0			mp5         { -1 1 3 5 7 9 }
fight_weapon M4					2.0 	0.0 	10.0	ballist		exp_F_Ballistic	1 		0			m4          { -1 1 4 7 }
fight_weapon Para				5.5 	0.0 	3.5		ballist		exp_F_Ballistic	1 		0			para        { -1 1 4 7 10 13 16 19 22 25 28}
fight_weapon M3_super_90		100.0 	0.0 	3.0		ballist		exp_F_Ballistic	1 		0			m3_super_90	{ -1 1 }
fight_weapon Duals				1.0	 	0.0 	6.0		ballist		exp_F_Ballistic	1 		0			duals       { -1 1 4 7 10 13 16 }
fight_weapon Awp				100.0 	0.0 	50.0	ballist		exp_F_Ballistic	1 		0			awp       	{ -1 1 }
fight_weapon Deagle				5.0 	0.0 	11.0	ballist		exp_F_Ballistic	1 		0			deagle     	{ -1 1 9 }


//Schilder:
fight_weapon Schild 			0.0 	0.3 	0.0		shield		exp_F_Defense	3		0
fight_weapon Trollschild_1      0.0 	0.3 	1.0		shield		exp_F_Defense	1		0
fight_weapon Schild_1       	0.0 	0.35 	1.0		shield		exp_F_Defense	2		0
fight_weapon Schild_2         	0.0 	0.4 	1.0		shield		exp_F_Defense	2		0.01
fight_weapon Metallschild 		0.0 	0.45	0.0		shield		exp_F_Defense	2		0.03
fight_weapon Schild_3         	0.0 	0.5 	1.0		shield		exp_F_Defense	2		0.04
fight_weapon Kristallschild 	0.0 	0.55 	0.0		shield		exp_F_Defense	2		0.06
fight_weapon Trollschild_2      0.0		0.55 	1.0		shield		exp_F_Defense	1		0
fight_weapon Schild_unq_1       0.0 	0.57	1.0		shield		exp_F_Defense	1.5		0.07
fight_weapon Schild_unq_2       0.0 	0.6 	1.0		shield		exp_F_Defense	1.5		0.08
fight_weapon Drachenschuppe     0.0 	0.65 	1.0		shield		exp_F_Defense	1		0.1
fight_weapon Trollschild_3      0.0 	0.65 	1.0		shield		exp_F_Defense	1		0
fight_weapon Trainierschild 	0.0 	0.21 	0.0		shield		exp_F_Defense	1		0

//Einhänder:
fight_weapon Keule				0.65 	0.0 	1.0		sword		exp_F_Sword		2.5		0
fight_weapon Dolch_2         	0.7 	0.0 	1.0		sword		exp_F_Sword		2.4		0.01
fight_weapon Axt_1 				0.75 	0.0 	1.0		sword		exp_F_Sword		2.3		0.02
fight_weapon Schwert_1          0.8 	0.0 	1.0		sword		exp_F_Sword		2.2		0.03
fight_weapon Axt_unq_1          0.82 	0.0 	1.0		sword		exp_F_Sword		2		0.05
fight_weapon Dolch_1     		1.0 	0.0 	1.0		sword		exp_F_Sword		1		0
fight_weapon Schwert 			1.0 	0.0 	1.0		sword		exp_F_Sword		2		0.07
fight_weapon Streitkolben  	    1.5 	0.0 	1.0		sword		exp_F_Sword		1		0
fight_weapon Axt_3            	1.5 	0.0 	1.0		sword		exp_F_Sword		1.5		0.08
fight_weapon Krumsaebel       	2.0		0.0 	1.0		sword		exp_F_Sword		1		0
fight_weapon Schwert_4         	2.0 	0.0 	1.0		sword		exp_F_Sword		1.2		0.12
fight_weapon Axt_unq_3          2.5 	0.0 	1.0		sword		exp_F_Sword		1		0.18
fight_weapon Trainierschwert	0.01 	0.0 	1.0		sword		exp_F_Sword		1		0

//Zweihänder:
fight_weapon Lanze_1            0.75 	0.0 	1.0		twohand		exp_F_Twohanded	1		0
fight_weapon Streitaxt     	   	1.2 	0.0 	1.0		twohand		exp_F_Twohanded	2.5		0
fight_weapon Schwert_2         	1.4 	0.0 	1.0		twohand		exp_F_Twohanded	2		0.02
fight_weapon Axt_2            	1.5 	0.0 	1.0		twohand		exp_F_Twohanded	2.3		0.03
fight_weapon Lanze_2       		1.7 	0.0 	1.0		twohand		exp_F_Twohanded	1		0
fight_weapon Axt_unq_2        	1.7 	0.0 	1.0		twohand		exp_F_Twohanded	1.5		0.07
fight_weapon Schwert_3         	1.9 	0.0 	1.0		twohand		exp_F_Twohanded	1.7		0.08
fight_weapon Lichtschwert 		2.2 	0.0 	1.0		twohand		exp_F_Twohanded	1.5		0.12
fight_weapon Axt_4            	2.5 	0.0 	1.0		twohand		exp_F_Twohanded	1.3		0.15
fight_weapon Zauberstab      	2.9 	0.0 	1.0		twohand		exp_F_Twohanded	1		0
fight_weapon Axt_unq_4          2.7 	0.0 	1.0		twohand		exp_F_Twohanded	1		0.2
fight_weapon Hellebarde       	2.7 	0.0 	1.0		twohand		exp_F_Twohanded	1		0
fight_weapon Trainier_2h_Schwert 0.01	0.0 	1.0		twohand		exp_F_Twohanded	1		0

//Definitionen für Waffenkombinationen (müssen unter den Waffendefinitionen stehen !!!)
fight_weapon_kombi	{ Axt_unq_1 Schild_unq_2 } { 2.0 1.5 }
fight_weapon_kombi	{ Axt_unq_2 Amulett_1 } { 2.0 0 }
fight_weapon_kombi	{ Axt_unq_3 Schild_unq_1 } { 2.0 1.5 }
fight_weapon_kombi	{ Axt_unq_4 Amulett_2 } { 2.0 0 }
fight_weapon_kombi	{ Schwert_2 Amulett_3 } { 2.5 0 }

// Verhältnisse zwischen den Völkern und Kreaturen
//
// def_hp_ratio <name> <owner> <HP Ratio> (<HP escape>)
// defaults:					1.0			   0.1

def_hp_ratio Zwerg 0 	2.0	0.15	;# Wiggle
def_hp_ratio Zwerg 1 	1.7	0.3		;# Voodoo
def_hp_ratio Zwerg 2 	2.2	0.1		;# Knocker
def_hp_ratio Zwerg 3 	1.5	0.5		;# Brain
def_hp_ratio Zwerg 4 	2.4			;# Vampy
def_hp_ratio Zwerg 5 	2.0			;# ???
def_hp_ratio Zwerg 6 	2.0			;# ???
def_hp_ratio Zwerg 7 	2.0			;# ???


def_hp_ratio Geisel			all 	0.8		0.0		;# Geisel
def_hp_ratio Wuker 			all 	1.3		0.0		;# Wuker war 0.8
def_hp_ratio Schwefelwuker	all 	1.8		0.0		;# Wuker war 1.4
def_hp_ratio Troll 			all 	2.3		0.1		;# Troll war 2.0
def_hp_ratio Riesenhamster 	all	   10.0		0.0		;# Riesenhamster
def_hp_ratio Riesenlaufrad 	all	   10.0		0.0		;# Riesenlaufrad
def_hp_ratio Alienpflanze	all		1.2		0.0		;# Alienpflanze
def_hp_ratio Fresspflanze	all		0.5		0.0		;# Fresspflanze
def_hp_ratio Kristallbrut	all		1.0		0.0		;# Kristallbrut
def_hp_ratio Lavabrut		all		1.7		0.0		;# Lavabrut war 1.5
def_hp_ratio Fenris			all	   10.0		0.0		;# Fenris
def_hp_ratio FenrisFuss		all	   20.0		0.0		;# FenrisFuss
def_hp_ratio ElfenFluegelA	all	    5.0		0.0		;# Elfenflügel (Riesenelfe)
def_hp_ratio ElfenFluegelB	all	    5.0		0.0		;# Elfenflügel (Riesenelfe)
def_hp_ratio ElfenFluegelC	all	    5.0		0.0		;# Elfenflügel (Riesenelfe)
def_hp_ratio ElfenFluegelD	all	    5.0		0.0		;# Elfenflügel (Riesenelfe)
def_hp_ratio Krake 			all 	2.0		0.0		;# Krake
def_hp_ratio Krake_			all 	2.0		0.0		;# Krake_
def_hp_ratio Drache			all    10.8		0.0		;# Drache
def_hp_ratio Drachenbaby	all 	2.5 	0.0		;# Drachenbaby
def_hp_ratio Spinne			all 	1.5		0.0		;# Spinne war 1.0
