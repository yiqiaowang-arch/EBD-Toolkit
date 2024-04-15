# Getting Started
The EBD-Toolkit is a set of tools for [Unity](https://unity.com/) and [Rhino](https://www.rhino3d.com/) & [Grasshopper](https://www.grasshopper3d.com/page/download-1) that allows you to perform agent-based simulations, cognitive walkthroughs and topological analysis on your architectural designs. You do not know how to code to make full use of the toolkit - but if you want to, you can modify it to your own liking.
To use it, we will first set up:
- **Unity Hub**: A program that allows you to manage Unity projects and different versions of the Unity Engine.
- **Git**: A tool that allows you to keep up to date with the latest changes of the EBD-Toolkit.
- **GitHub Desktop**: A user interface to use git without needing to learn the command line.
- **Rhino 7**: A CAD software that supports parametric modeling through the Grasshopper plugin.
## 1. Installing GitHub Desktop
GitHub desktop is a user interface for the version-control tool _git_. We will use _git_ to download the latest version of the EBD-Toolkit and get the newest updates later.
We only need to understand one single action it allows us to do: _pull_. Pull means: "Get the latest changes of the software, compare them to my current state, and integrate changes if there are any".
Visit [GitHub Desktop's website](https://desktop.github.com/) and click the _Download for [Windows | Mac]_ button. Install and open the tool. Upon opening, you will be prompted to create or sign into you GitHub account. Please create one if you have not already done so.
## 2. Installing the EBD-Toolkit Project
In GitHub Desktop, choose _File > Clone repository... > URL_ and enter the following URL: `https://github.com/rabaur/EBD-Toolkit.git`. Below, you will see where the toolkit will be saved on your system. Then, click _clone_.
After the download, you can verify that the project has been saved to the indicated path.
## 3. Installing the Unity Hub
Now that we have downloaded the project, we need to install Unity Engine to open and view it. Visit [Unity's website](https://unity.com/pricing), then choose _Student & hobbyist_ > _Personal_ > _1. Download the Unity Hub > Download for [Windows | Mac | Linux]_, depending on your platform.
After opening Unity Hub, you will be prompted to sign in. Please create a Unity ID if you have not already done so.
### 3.1 Installing Unity Engine
After logging in, we first need a version of the Unity Engine. Go to _Installs > Add > Recommended Release: Unity 2022.3.XX_. You do not need to select anything after that, and can simply press _Done_. This will download and install the corresponding Unity version on your system.
>[!WARNING]
>Generally, it should be possible you use a newer or older version of Unity of the same major version (everything that starts with 2022.x.xxxx), or even older or newer versions. However, at the time of writing this tutorial, only the version indicated above has been tested.

### 3.2 Opening the Unity Project
After the installation has finished, we are ready to add EBD-Toolkit project to the project list. Go to _Projects > Add_, and then choose the location of the project folder you have cloned in 2. In the newly added project, add the Unity version you have just installed, and click on the project to open it.
>[!NOTE]
>Depending on your exact version of Unity, you will see a prompt asking you if you want to upgrade the project to a newer version of Unity. Choose _Confirm_

The first time you open the project, the start-up might take a few minutes.


# Cognitive Walkthroughs in Virtual Reality
The EBD-Toolkit provides a set of scripts to perform virtual walkthroughs in scenes where you visualize your design.
>[!TIP]
>In Unity Engine, _objects_ are the elements like characters or props, _scenes_ are the virtual environments where they exist, and _scripts_, written in programming languages like C#, define their behavior. Objects are placed in scenes, and scripts control their interactions, movements, and responses.

This tutorial will show you how to set up a virtual walkthrough experiment using a scene template that you can easily modify and extend.

# Agent-Based Modeling to Simulate Occupants' Behavior

# I have a question, found a bug or have a suggestion regarding the toolkit
