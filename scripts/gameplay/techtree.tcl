//name 		cat 	type		Era	Flags
{
Feuerstelle 	Food	Location 	1 	{}
	//materials
	{}
	//Tools
	{}
	//Blueprints
	{}
	//Places
	{}
}

//name 		cat 	type		Era	Flags
{
Grillpilz	Food	Material 	1 	{}
	//materials
	{Pilz}
	//Tools
	{}
	//Blueprints
	{}
	//Places
	{Feuerstelle}
} 

//name 		cat 	type		Era	Flags
{
Hauklotz 	Food	Location 	1 	{}
	//materials
	{Holz}
	//Tools
	{}
	//Blueprints
	{}
	//Places
	{Feuerstelle}
}

//name 		cat 	type		Era	Flags
{
Holzzaun 	Food	Location 	1 	{}
	//materials
	{Holz}
	//Tools
	{}
	//Blueprints
	{}
	//Places
	{Hauklotz}
}

//name 		cat 	type		Era	Flags
{
Pilz 		Food	Material	1 	{reproduces}
	//materials
	{}
	//Tools
	{}
	//Blueprints
	{}
	//Places
	{Holzzaun}
}

//name 		cat 	type		Era	Flags
{
Holz 		Food	Material	1 	{}
	//materials
	{Pilz}
	//Tools
	{}
	//Blueprints
	{}
	//Places
	{}
}
